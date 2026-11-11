# SecretSaver

A small tool to share a password through a link. The password can be viewed a limited number of times, within a limited time, and is deleted afterwards. No third party is involved: the secret only lives in this application's own database, and only until it is used up or expires.

Built for the CBYTE "Password Sharing Tool" assessment as an ASP.NET Core MVC monolith with Tailwind CSS, using a layered (onion) architecture.

## Features

| User story | How it works |
|---|---|
| 1. Create a link for a password | Paste the password on the start page, choose a lifetime and a number of views, and click **Create link**. |
| 2. View the password after opening the link | The link opens a confirm page. **Reveal secret** shows the password, with a copy button. |
| 3. Delete the password after the link is opened | The row is deleted from the database as soon as the last allowed view is used, before the page is sent to the browser. |
| 4. (optional) Maximum lifetime | 1 hour, 1 day, 3 days or 7 days. Expired secrets can't be revealed and are deleted when someone tries. |
| 5. (optional) Maximum number of uses | 1 to 10 views. Each reveal counts as one view. |

Links that are used up, expired or never existed all show the same neutral "This link is no longer available" page, so a link can't be used to find out whether a secret ever existed.

## Usage

1. Start the app (see [Run](#run)) and open the URL it prints, for example `http://localhost:5032`.
2. Enter a password, choose **Expires after** and **Number of views**, and click **Create link**.
3. Copy the link from the "Your link is ready" page and send it to the recipient. The page is not stored, so copy the link before leaving it.
4. The recipient opens the link, clicks **Reveal secret** and saves the password.

## Architecture

The class diagram is in `SecretSaver diagram.drawio`.

| Project | Role | References |
|---|---|---|
| `src/SecretSaver.Domain` | The `Secret` entity and its rules | nothing |
| `src/SecretSaver.Application` | `ISecretRepository`, `SecretService` | Domain |
| `src/SecretSaver.Infrastructure` | ADO.NET data access: SQL repository, database providers, provider choice | Application |
| `src/SecretSaver.Web` | MVC controllers, views, Tailwind CSS, `Program.cs` | Application, Infrastructure |

## Design decisions

- **ASP.NET Core instead of Laravel.** The assignment suggests Laravel. I have worked exclusively in C# in recent years and not in PHP, and with the short timeline I didn't want to spend time on setting up tooling and refreshing PHP knowledge. Since the assessment is about my own technical skills, I agreed with the reviewer to build it in C# instead: ASP.NET Core MVC (.NET 10), with Tailwind CSS as the assignment asks. It is a monolith: server-rendered Razor views, no separate front end or API.
- **Onion architecture.** Dependencies only point inward. The Domain knows nothing about storage or the web; the Application layer defines what it needs from storage (`ISecretRepository`) and Infrastructure implements it. Swapping the storage or the UI doesn't touch the business rules.
- **Rich domain model.** `Secret` guards its own rules. Rules that always hold (a value, at least 1 allowed view, never more views than allowed) are checked for every secret, in the constructor. `Create` adds the rule that only applies to new secrets: the expiry must lie in the future. `Restore` rebuilds a secret read from the database with the same checks, so it can't produce an invalid secret, but an already expired one loads fine. After that, the state only changes through `RegisterView()`.
- **Plain ADO.NET instead of an ORM.** The data access is small: one table, four queries. Writing it against the ADO.NET base classes keeps it explicit and makes the multi-database design visible (see [Database](#database)).
- **Database provider chosen by configuration.** `Database:Provider` maps to the `Stores` enum. An unknown value stops the app at startup instead of failing on the first request.
- **Reveal is a POST, not a GET.** Opening a link only shows a confirm page. Link previews in chat apps and mail scanners send GET requests, so they can't use up a one-time link by accident. The POST is protected with an anti-forgery token.
- **No caching of sensitive pages.** The "link created" and "your secret" responses send `Cache-Control: no-store`, so the Back button doesn't show the secret again. All pages send `no-referrer`, so the link doesn't leak to other sites.
- **No error pages during normal use.** Form input is validated with clear messages, broken links show a "Page not found" page, and unexpected errors show a friendly error page.

## Requirements

- .NET 10 SDK
- Node.js (for the Tailwind CSS build)

## Run

```bash
dotnet run --project src/SecretSaver.Web
```

The first build runs `npm install` in `src/SecretSaver.Web`. Every build compiles `Styles/app.css` to `wwwroot/css/app.css`. While editing views, run `npm run watch:css` in `src/SecretSaver.Web` to rebuild the CSS on every change.

## Database

Set in `src/SecretSaver.Web/appsettings.json`:

- `Database:Provider`: name of a value in the `Stores` enum, case-insensitive. Currently only `Sqlite`. An unknown value stops the app at startup.
- `ConnectionStrings:Default`: `Data Source=secretsaver.db`

The table is created on startup (`CREATE TABLE IF NOT EXISTS`), so there are no migrations to run.

### How data access is organised

Data access uses plain ADO.NET (`System.Data.Common`), without an ORM. It has two parts:

- `SqlSecretRepository` contains all queries. It only uses `DbConnection`, `DbCommand` and `DbDataReader`, so the same code works for every SQL database.
- An `IDatabaseProvider` per database supplies the parts that differ: the connection (`CreateConnection()`) and the table definition (`CreateSchemaSql`).

Adding another SQL database (SQL Server, MySQL, PostgreSQL) means writing a new `IDatabaseProvider`. The repository stays the same.

### Adding a database provider

The steps below use SQL Server as an example.

1. **Add the ADO.NET driver** to the Infrastructure project:

   ```bash
   dotnet add src/SecretSaver.Infrastructure package Microsoft.Data.SqlClient
   ```

   Use a package version that supports .NET 10.

2. **Add a value to the `Stores` enum** in `src/SecretSaver.Infrastructure/Stores.cs`:

   ```csharp
   public enum Stores
   {
       Sqlite,
       SqlServer
   }
   ```

3. **Create the provider** in its own folder, next to `Persistence/Sqlite/`. For example `src/SecretSaver.Infrastructure/Persistence/SqlServer/SqlServerDatabaseProvider.cs`:

   ```csharp
   using System.Data.Common;
   using Microsoft.Data.SqlClient;

   namespace SecretSaver.Infrastructure.Persistence.SqlServer;

   internal sealed class SqlServerDatabaseProvider(string connectionString) : IDatabaseProvider
   {
       public DbConnection CreateConnection() => new SqlConnection(connectionString);

       public string CreateSchemaSql => """
           IF OBJECT_ID(N'Secrets', N'U') IS NULL
           CREATE TABLE Secrets (
               Id         NVARCHAR(36)  NOT NULL PRIMARY KEY,
               Value      NVARCHAR(MAX) NOT NULL,
               ExpiryDate NVARCHAR(40)  NOT NULL,
               Views      INT           NOT NULL,
               MaxViews   INT           NOT NULL
           )
           """;
   }
   ```

   `CreateSchemaSql` must be safe to run on every startup, so it must not fail when the table already exists. SQLite uses `CREATE TABLE IF NOT EXISTS`; SQL Server needs the `IF OBJECT_ID(...)` check.

   Make the column types match the values the repository writes. The example assumes `Id` and `ExpiryDate` are stored as text (`ExpiryDate` as ISO 8601, format `"O"`), like in the SQLite table. If the repository passes `Id` as a `Guid` instead, use `UNIQUEIDENTIFIER`.

4. **Map the enum value to the provider** in the switch in `src/SecretSaver.Infrastructure/DependencyInjection.cs`:

   ```csharp
   IDatabaseProvider provider = store switch
   {
       Stores.Sqlite => new SqliteDatabaseProvider(connectionString),
       Stores.SqlServer => new SqlServerDatabaseProvider(connectionString),
       _ => throw new NotSupportedException($"Database provider '{store}' is not supported.")
   };
   ```

5. **Point the configuration at it** in `appsettings.json` (or with the `Database__Provider` and `ConnectionStrings__Default` environment variables):

   ```json
   "Database": {
     "Provider": "SqlServer"
   },
   "ConnectionStrings": {
     "Default": "Server=localhost;Database=SecretSaver;Trusted_Connection=True;TrustServerCertificate=True"
   }
   ```

6. **Run the app** and check that the `Secrets` table is created. Then create a secret and open its link.

#### Keep the repository's SQL portable

`SqlSecretRepository` is shared by every provider. So it only uses SQL that works on all of them:

- Plain `SELECT`, `INSERT`, `UPDATE` and `DELETE` with `WHERE Id = @id`. No `LIMIT`, `TOP`, `RETURNING` or other database-specific syntax.
- Parameters named with `@`, through `command.AddParameter(...)`. SQLite, SQL Server and MySQL drivers all support this.

If a new database really needs different SQL, add a property for it to `IDatabaseProvider`, like `CreateSchemaSql`, instead of checking the database type inside the repository.

## Known limitations and next steps

These are deliberate choices to keep the scope of the assessment small:

- **Secrets are stored as plain text.** Anyone with access to the database file can read unused secrets. Next step: encrypt each secret with its own key and put that key only in the link, so the database alone is useless.
- **Two simultaneous reveals of a one-time link can both succeed.** The view count is read, changed and written in separate steps. Next step: make the update conditional (`UPDATE ... WHERE Id = @id AND Views = @viewsWhenRead`) and treat 0 affected rows as "no longer available".
- **Expired links that nobody opens stay in the database** until someone tries to open them. Next step: a background job that deletes expired rows periodically.
- **Refreshing the "link created" page resubmits the form** and creates a second link. Next step: redirect after the POST (Post/Redirect/Get).

## Time spent

About 6 hours in total

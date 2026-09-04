using System.Runtime.CompilerServices;

// IamDbContext is internal to Corely.IAM, so the factory that returns one cannot be public.
[assembly: InternalsVisibleTo("Corely.IAM.DataAccessMigrations.Cli")]

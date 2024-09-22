# Recreate Database
To create a new database run the following from the root folder of the solution.
```powershell
dotnet ef migrations add InitialMigration --project ./ProcessTrackerService.Infrastructure --startup-project ./ProcessTrackerService
dotnet ef database update --project ./ProcessTrackerService.Infrastructure --startup-project ./ProcessTrackerService
```
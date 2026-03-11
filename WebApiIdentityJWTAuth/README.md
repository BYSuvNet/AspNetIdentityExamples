dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore 
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet tool install --global dotnet-ef 
dotnet ef migrations add InitialCreate

Uppdatera appsettings.json med JWT config
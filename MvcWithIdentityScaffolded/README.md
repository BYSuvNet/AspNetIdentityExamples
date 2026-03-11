dotnet new mvc --auth Individual -n MvcWithIdentityScaffolded

dotnet add package Microsoft.VisualStudio.Web.CodeGeneration.Design

dotnet aspnet-codegenerator identity --dbContext ApplicationDbContext --force
MSBuild /p:Configuration=Release

dotnet pack -o ..\..\artifacts -c Release .\src\XxlJob.Core
dotnet pack -o ..\..\artifacts -c Release .\src\XxlJob.AspNetCoreHost
nuget pack .\src\XxlJob.WebApiHost -OutputDirectory artifacts -Properties Configuration=Release

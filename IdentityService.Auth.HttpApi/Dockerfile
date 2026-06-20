FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5008

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 【核心修复】把所有子项目的 .csproj 全都提前复制进去
COPY ["IdentityService.Auth.HttpApi/IdentityService.Auth.HttpApi.csproj", "IdentityService.Auth.HttpApi/"]
COPY ["IdentityService.Auth.Application/IdentityService.Auth.Application.csproj", "IdentityService.Auth.Application/"]
COPY ["IdentityService.Auth.Domain/IdentityService.Auth.Domain.csproj", "IdentityService.Auth.Domain/"]
COPY ["IdentityService.Auth.Infrastructure/IdentityService.Auth.Infrastructure.csproj", "IdentityService.Auth.Infrastructure/"]

# 此时依赖完整，Restore 能够完美成功并被 Docker 牢牢缓存
RUN dotnet restore "./IdentityService.Auth.HttpApi/IdentityService.Auth.HttpApi.csproj"

# 复制源码
COPY . .
WORKDIR "/src/IdentityService.Auth.HttpApi"
RUN dotnet build "./IdentityService.Auth.HttpApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./IdentityService.Auth.HttpApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.Auth.HttpApi.dll"]
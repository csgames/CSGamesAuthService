FROM microsoft/dotnet:2.1-sdk as dotnet-build
WORKDIR /dotnet/STS

COPY . .

RUN dotnet restore
RUN dotnet publish -c Release -o out -r linux-x64

FROM microsoft/dotnet:2.1-runtime-stretch-slim
WORKDIR /Volumes/STS
COPY --from=dotnet-build /dotnet/STS/out .
ENTRYPOINT ["dotnet", "STS.dll"]
EXPOSE 5555

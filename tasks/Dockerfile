FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BUILD_VERSION
WORKDIR /src
COPY src /src
RUN dotnet publish --configuration Release --nologo --output publish -p:Version=${BUILD_VERSION}

FROM public.ecr.aws/lambda/dotnet:6 AS runtime
COPY --from=build /src/publish ${LAMBDA_TASK_ROOT}
CMD [ "TodayInDestiny2.Tasks::TodayInDestiny2.Tasks.LamdaEntryPoints::RefreshCurrentActivitiesHandler" ]
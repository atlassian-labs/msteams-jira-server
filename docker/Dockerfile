FROM mcr.microsoft.com/dotnet/sdk:6.0

RUN curl -sL https://deb.nodesource.com/setup_16.x  | bash -
RUN apt-get -y install nodejs
# install zip command to compress the output of build
RUN apt-get -y install zip
# install xmlstarlet command to edit XML files
RUN apt-get -y install xmlstarlet
# install jq command for processing JSON files
RUN apt-get -y install jq
# install jdk (needed by sonarqube dotnet tool)
RUN apt-get install -y openjdk-17-jre
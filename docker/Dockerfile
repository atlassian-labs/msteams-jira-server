FROM mcr.microsoft.com/dotnet/sdk:8.0

RUN curl -sL https://deb.nodesource.com/setup_18.x  | bash -
RUN apt-get -y install nodejs
# install zip command to compress the output of build
RUN apt-get -y install zip
# install xmlstarlet command to edit XML files
RUN apt-get -y install xmlstarlet
# install jq command for processing JSON files
RUN apt-get -y install jq
# install jdk (needed by sonarqube dotnet tool)
RUN apt-get install -y openjdk-17-jre
# install Angular tests related packages
RUN apt-get update
RUN apt-get install -y wget gnupg --no-install-recommends
RUN wget -q -O - https://dl.google.com/linux/linux_signing_key.pub | apt-key add -
RUN sh -c 'echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google-chrome.list'
RUN apt-get update
RUN apt-get install -y google-chrome-stable --no-install-recommends
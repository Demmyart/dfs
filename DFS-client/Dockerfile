FROM node:10.12.0-alpine

RUN mkdir -p /usr/src/app
WORKDIR /usr/src/app

RUN npm install -g @angular/cli

COPY package.json /usr/src/app/package.json

RUN npm install

COPY . /usr/src/app
CMD ng serve --host 0.0.0.0
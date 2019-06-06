FROM mono:latest
RUN mkdir /StorageServer
COPY . /StorageServer
RUN cd /StorageServer/
RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
RUN echo "deb http://download.mono-project.com/repo/debian wheezy main" | tee /etc/apt/sources.list.d/mono-xamarin.list
RUN apt-get update
RUN apt install mono-xsp4 -y

WORKDIR /StorageServer/

CMD ["/bin/sh","/StorageServer/start_server.sh"]

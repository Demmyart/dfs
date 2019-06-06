#/bin/sh

echo "Launching Storage server at port $1"

(sleep 10 && curl "http://127.0.0.1:$1/api/client/fileinfo?path=") &

echo "$1" > Port

/usr/bin/mono /usr/lib/mono/4.5/xsp4.exe --port $1 --address 0.0.0.0 --nonstop --verbose




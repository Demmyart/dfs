#/bin/sh

echo "Launching Storage server at port $SERVER_PORT"

(sleep 5 && curl "http://127.0.0.1:$SERVER_PORT/api/client/fileinfo?path=" 2>/dev/null) &

echo "$SERVER_PORT" > Port

/usr/bin/mono /usr/lib/mono/4.5/xsp4.exe --port $SERVER_PORT --address 0.0.0.0 --nonstop --verbose




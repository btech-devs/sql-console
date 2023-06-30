openssl genrsa -out ./sql-console-private.pem 1024
openssl rsa -in ./sql-console-private.pem -pubout -out ./sql-console-public.pem
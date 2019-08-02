#!/bin/sh

echo "update fl_server_dev.fl_config set data = '" > __temp.cql
cat $1 >> __temp.cql
echo "' where key = 0;" >> __temp.cql
cqlsh -f __temp.cql
rm -f __temp.cql

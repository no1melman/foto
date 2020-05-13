#!/bin/bash

cat > $PROP_FILE << EOF
#user  nobody;
worker_processes  auto;
worker_rlimit_nofile 100000;
#error_log  logs/error.log;
#error_log  logs/error.log  notice;
#error_log  logs/error.log  info;
error_log /dev/stdout info;
#pid        logs/nginx.pid;
events {
    worker_connections  4000;
    use epoll;
    multi_accept on;
}
http {
    open_file_cache max=20000000 inactive=20s;
    open_file_cache_valid 30s;
    open_file_cache_min_uses 2;
    open_file_cache_errors on;
    include       mime.types;
    default_type  application/json;
    sendfile        on;
    access_log /dev/stdout;
    tcp_nodelay on;
    # reduce the data that needs to be sent over network -- for testing environment
    gzip on;
    # gzip_static on;
    gzip_min_length 10240;
    gzip_comp_level 1;
    gzip_vary on;
    gzip_disable msie6;
    gzip_proxied expired no-cache no-store private auth;
    gzip_types
        # text/html is always compressed by HttpGzipModule
        text/css
        text/javascript
        text/xml
        text/plain
        text/x-component
        application/javascript
        application/x-javascript
        application/json
        application/xml
        application/rss+xml
        application/atom+xml
        font/truetype
        font/opentype
        application/vnd.ms-fontobject
        image/svg+xml;
    # allow the server to close connection on non responding client, this will free up memory
    reset_timedout_connection on;
    # request timed out -- default 60
    client_body_timeout 10;
    client_max_body_size 200M;
    # if client stop responding, free up memory -- default 60
    send_timeout 2;
    # server will close connection after this time -- default 75
    keepalive_timeout 30;
    # number of requests client can make over keep-alive -- for testing environment
    keepalive_requests 100000;
    server_tokens off;
    server {
        listen       ${NGINX_PORT};
        listen       [::]:${NGINX_PORT};
        server_name  localhost;
        
        location /healthz {
            return 200 "healthy\n";
        }
        location / {
            root    /usr/share/nginx/html;
            index   index.html;
            try_files \$uri /index.html;
        }
        location /api/ {
            proxy_pass              http://${PROXY_HOST};
            proxy_http_version      1.1;
            proxy_redirect          off;
            proxy_set_header        Host            \$http_host;
            proxy_set_header        X-Real-IP       \$remote_addr;
            proxy_set_header        X-Forwared-For  \$proxy_add_x_forwarded_for;
            proxy_connect_timeout   5s;
            proxy_socket_keepalive  on;
            proxy_read_timeout      10s;
            proxy_send_timeout      10s;
        }
    }
}
EOF

cat /etc/nginx/nginx.conf

nginx -g 'daemon off;'

exec "$@"
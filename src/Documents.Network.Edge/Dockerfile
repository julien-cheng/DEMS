FROM nginx:1.13.8

COPY ./nginx.conf /etc/nginx/nginx.conf

WORKDIR /usr/src

ADD start.sh /usr/src/
RUN chmod +x /usr/src/start.sh

ENTRYPOINT ./start.sh
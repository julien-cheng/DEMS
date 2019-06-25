FROM ubuntu:precise
RUN apt-get update 
#RUN apt-get -y upgrade
RUN DEBIAN_FRONTEND=noninteractive apt-get -y install nano openssh-server sudo netcat

RUN echo %sudo	ALL=NOPASSWD: ALL >> /etc/sudoers && \
    mkdir /var/run/sshd && \
    mkdir /home/ubuntu && \
    useradd -G sudo -d /home/ubuntu ubuntu && \
    chown ubuntu:ubuntu /home/ubuntu

#RUN echo ubuntu:ubuntu | chpasswd && \
ADD authorized_keys /home/ubuntu/.ssh/authorized_keys

RUN sudo chsh ubuntu -s /bin/bash && \
    chown -R ubuntu:ubuntu /home/ubuntu/.ssh

EXPOSE 22

CMD    ["/usr/sbin/sshd", "-D"]
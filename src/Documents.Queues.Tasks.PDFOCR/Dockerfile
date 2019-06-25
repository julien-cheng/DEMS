FROM mcr.microsoft.com/dotnet/core/runtime:2.2-bionic AS runtime
RUN \
  apt-get update && apt-get -y install software-properties-common

RUN \
  add-apt-repository -y ppa:malteworld/ppa
RUN \
  apt-get update && apt-get -y install \
    build-essential \
    tesseract-ocr \
    tesseract-ocr-eng\
    ruby \
    pdftk \
    python3-pip \
    python-pip \
    ocrmypdf

RUN \
  apt-get install \
    libexempi3 \
    pngquant

RUN \
  export PATH=$HOME/.local/bin:$PATH
ENV LANG C.UTF-8
RUN \
  export LC_ALL=C.UTF-8
RUN \
  export LANG=C.UTF-8
RUN \
  pip3 install --user ocrmypdf

WORKDIR /app
COPY --from=documents:localbuild /app/src/Documents.Queues.Tasks.PDFOCR/bin/Release/netcoreapp2.0/publish/ /app
RUN chmod +x /app/pdfocr.rb

ENTRYPOINT ["dotnet", "Documents.Queues.Tasks.PDFOCR.dll"]

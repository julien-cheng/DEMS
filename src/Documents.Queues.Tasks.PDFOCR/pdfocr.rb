#!/usr/bin/ruby

# Copyright (c) 2010 Geza Kovacs
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
# THE SOFTWARE.

require 'optparse'
require 'tmpdir'

def shell_escape(s)
    "'" + s.gsub("'", "'\\''") + "'"
end

def sh(c, *args)
  outl = []
  if args.length > 0
    c = shell_escape(c) + ' '
    c << args.map {|w| shell_escape(w)}.join(' ')
  end
  IO.popen(c) { |f|
    while not f.eof?
      tval = f.gets
      puts tval
      outl.push(tval)
    end
  }
  return outl.join("")
end

def writef(fn, c)
  File.open(fn, "w") { |f|
    f.puts(c)
  }
end

def rmdir(dirn)
  Dir.foreach(dirn) { |fn|
    if fn == "." or fn == ".."
      next
    end
    fn = File.expand_path(dirn+"/"+fn)
    if File.directory?(fn)
      rmdir(fn)
    else
      File.delete(fn)
    end
  }
  Dir.delete(dirn)
end

appname = 'pdfocr'
version = [0,2,0]
infile = nil
outfile = nil
deletedir = true
deletefiles = true
language = 'eng'
tmp = nil

optparse = OptionParser.new { |opts|
opts.banner = <<-eos
Usage: #{appname} -i input.pdf -o output.pdf
#{appname} adds text to PDF files using the ocropus, cuneiform, or tesseract OCR software
eos

  opts.on("-i", "--input [FILE]", "Specify input PDF file") { |fn|
    infile = fn
  }

  opts.on("-o", "--output [FILE]", "Specify output PDF file") { |fn|
    outfile = fn
  }

  opts.on_tail("-h", "--help", "Show this message") {
    puts opts
    exit
  }

  opts.on_tail("-v", "--version", "Show version") {
    puts version.join('.')
    exit
  }

}

optparse.parse!(ARGV)

if not infile or infile == ""
  puts optparse
  puts
  puts "Need to specify an input PDF file"
  exit
end

if infile[-3..-1].casecmp("pdf") != 0
  puts "Input PDF file #{infile} should have a PDF extension"
  exit
end

#baseinfile = infile[0..-5]

#if not baseinfile or baseinfile == ""
#  puts "Input file #{infile} needs to have a name, not just an extension"
#  exit
#end

if not File.file?(infile)
  puts "Input file #{infile} does not exist"
  exit
end

infile = File.expand_path(infile)

if not outfile or outfile == ""
  puts optparse
  puts
  puts "Need to specify an output PDF file"
  exit
end

if outfile[-3..-1].casecmp("pdf") != 0
  puts "Output PDF file should have a PDF extension"
  exit
end

if outfile == infile
  puts "Output PDF file should not be the same as the input PDF file"
  exit
end

if File.file?(outfile)
  puts "Output file #{outfile} already exists"
  exit
end

outfile = File.expand_path(outfile)


if not deletedir
  if not File.directory?(tmp)
    puts "Working directory #{tmp} does not exist"
	exit
  else
    tmp = File.expand_path(tmp)+"/pdfocr"
    if File.directory?(tmp)
      puts "Directory #{tmp} already exists - remove it"
      exit
    else
      Dir.mkdir(tmp)
    end
  end
else
  tmp = Dir.mktmpdir
end
if `which pdftk` == ""
  puts "pdftk command is missing. Install the pdftk package"
  exit
end
puts "Input file is #{infile}"
puts "Output file is #{outfile}"
puts "Using working dir #{tmp}"

puts "Getting info from PDF file"

puts

pdfinfo = sh "pdftk", infile, "dump_data"

if not pdfinfo or pdfinfo == ""
  puts "Error: didn't get info from pdftk #{infile} dump_data"
  exit
end

puts

begin
  pdfinfo =~ /NumberOfPages: (\d+)/
  pagenum = $1.to_i
rescue
  puts "Error: didn't get page count for #{infile} from pdftk"
  exit
end

if pagenum == 0
  puts "Error: there are 0 pages in the input PDF file #{infile}"
  exit
end

writef(tmp+"/pdfinfo.txt", pdfinfo)

puts "Converting #{pagenum} pages"

numdigits = pagenum.to_s.length

puts "OCR MY PDF!"
sh "ocrmypdf", "--force-ocr", "--output-type", "pdf" ,"-l", language, infile, outfile
if deletefiles
 puts "Cleaning up temporary files"
 rmdir(tmp)
end
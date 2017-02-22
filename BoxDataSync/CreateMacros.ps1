<#
.SYNOPSIS
	This is a simple powershell script that performs forwards and backwards string replacements within a file ala macro preprocessing in C.
	Read this help via: powershell Get-Help .\CreateMacros.ps1 -full

.DESCRIPTION
	Four parameters are required in the following order including:
	 1) mapfile: a text file containing only name=value pairs delimited by a single equal sign (=) character, one name=value per line
	 2) source: a file containing "macros" to be expanded (strings to be replaced by their values)
	 3) destination: the file name to be written out 
	 4) direction: either forward or reverse. Forward implies macro substitution with the variable values, reverse puts the macros back

	 **Note: if mutiple variables map to the same value or the value appears in the text in other places, reverse processing will not result
	 in reproducing the original file

	 ***WARNING: Destination file will be overwritten WITHOUT WARNING. You've been warned!

.EXAMPLE
	C:\PS> CreateMacros "mapfile.txt" "sourcefile.txt" "destination.txt" "forward"

.NOTES
	Author: Jamie Anne Harrell
	Date:   February 21, 2017
	License: MIT / Open Source 

	Copyright (c) 2017 Emory Goizueta Business School
	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in all
	copies or substantial portions of the Software.
	
	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
	SOFTWARE. 

#>
if ($args.Length -ne 4) {
	$ick = "usage: CreateMacros [mapfile] [source] [destination] [direction=forward|reverse]"
	$ick
	exit
}

$replacements = $args[0]
$source = $args[1]
$destination = $args[2]
$direction = $args[3]

if (-NOT (Test-Path $replacements)) {
	$ick = "Cannot load replacements file:" + $replacements
	$ick
	exit
}

if (-NOT (Test-Path $source)) {
	$ick = "Cannot load source file:" + $source
	$ick
	exit
}

if (($direction -ne 'forward') -And ($direction -ne 'reverse')) {
	$ick = "Direction must be either forward or reverse"
	$ick 
	exit
}

$lookupTable = @{}
$in = ""
$out = ""
$reader = [System.IO.File]::OpenText($replacements)
try {
	for() {
		$line = $reader.ReadLine()
		if ($line -eq $null) { break }
		$thevar = $line -split '='
		if($direction -eq 'forward') 
			{
			$lookupTable[$thevar[0]] = $thevar[1] 
			} 
			else
			{
			$lookupTable[$thevar[1]] = $thevar[0] 
			}
		}
	}
finally {
	$reader.Close()
	}

$reader = [System.IO.File]::OpenText($source)
try {
	$count=0
	for() {
		$count += 1
		$line = $reader.ReadLine()
		if ($line -eq $null) { break }
		$in += $line
		$in += "`n"
		$lookupTable.GetEnumerator() | ForEach-Object {
			if ($line -cmatch $_.Key)
			{
				$line = $line -creplace $_.Key, $_.Value
			}
		}
		if ($count -gt 1) { $out+="`r`n" }
		$out += $line
		#$out += "`r`n"
		}
	}
finally {
	$reader.Close()
	}

$out | Out-File $destination -NoNewline -Encoding ASCII
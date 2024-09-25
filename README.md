# WPDtool
This tool allows you to unpack and repack the WPD type files from the FINAL FANTASY XIII trilogy. 

The wpd files can have any one of the following extensions:
<br>``.bin, .wdb, .wpd, .wpk, .xfv, .xgr, .xwb, .xwp``

Some of the WPD files will have a paired IMGB file that contains raw image data without a header. this tool will unpack these image data and will also assign a valid DDS header to these image data, thereby allowing the image to be vieweable in a compatible image viewing software.

The program should be launched from command prompt with any one of these following argument switches along with the input file:
<br>``-u`` Unpacks the WPD file
<br>``-r`` Repacks the unpacked WPD folder to a WPD file

Commandline usage examples:
<br>``WPDtool.exe -u "system.win32.xgr" ``
<br>``WPDtool.exe -r "_system.win32.xgr" ``

Note: For the ``-r`` switch, the unpacked WPD folder name is specified in the example. the ``_`` in the name indicates the name of the unpacked folder.

### Important
- Repacking is supported only for the PC version WPD IMGB files.
- Each WPD/WDB record and its extension (if present) will all be written into a !!WPD_Record.txt file. this file will be present in the unpacked folder.
- The first number present on the !!WPD_Record.txt file is the total number of records or files packed inside the WPD/WDB file. after this number, the record names will all be written in a straight order as it was present on the WPD/WDB file and each record name will have a `` |-| `` symbol which separates the record and its extension. if the extension is written null, then that particular record or file does not have a extension. this would be the case for all records that are unpacked from ``.wdb`` files.
- If you are replacing the DDS images with the repack function, then make sure that the image dimension, DDS Compression/ Pixel format and the mip count of your image are all similar to the original image file.
- The Xbox 360 version image data is swizzled and due to this swizzled nature, this tool will not unpack those images correctly.

## For Developers
- This tool makes use of this following reference library:
<br>**IMGBlibrary** - https://github.com/Surihix/IMGBlibrary

- Refer to this [page](https://github.com/LR-Research-Team/Datalog/wiki/WPD-Pack-files) for information about the the WPD's file structure.

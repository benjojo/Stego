Stego
=====

Some insane way of hiding messages in jpeg artifacts.

This program will hide messages in images but in a diffrent way from the standard "Change the data in the LSB" style.

This one hides it in jpeg artifacts.

Orig:

![OrigImage](/examples/t.jpg "Optional title")

After being binded with the message "Hello World"

![OrigImage](/examples/done.jpg "Optional title")


You may have noticed that the jpeg quality drops, this is because I use the artifacts to put the messages inside so they have to exist.


Anyway this was made in the "Does this idea actually work" kind of mind set so don't really expect it to work.

Program usage is:

To Encode
``Steg.exe input.jpg "message to encode"``

To Decode
``Steg.exe done.jpg``

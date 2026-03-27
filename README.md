# JAXBaseV1
A modern take on XBase
<BR>
<BR>
The JAXBase Project is an attempt to create a modern version of the venerable XBase language. It is highly object oriented and most legacy/MS-DOS commands and last-century paradigms have been removed.  The code will become open-source no later than Version 0.8 release and be under the GNU General Public License version 2 (GPL-2.0).
<BR>
<BR>
### Current Status
This is JAXBase V0.5 and has been moved to this repository early so that the code can be reviewed and suggestions on direction can be made.  I'm not a seasoned C# developer, and you'll likely see a progression of sophistication in the code base.<br>
<br>
Please go to [Project](https://github.com/jlwalker-dev/JAXBaseV1/wiki/Project) for more information.
<br>
<br>
## Differences from Most XBase Dialects
•	Data sessions are addressable in many commands.  ***USE table IN 0 SESSION 2*** opens the table in the lowest open work area of data session 2.<br><br>
•	The @ commands, color pairs, and color schemes no longer exist.<br><br>
•	GUI Form objects will have basic text-only auto-conversion in Version 1 for Linux.<br><br>
•	Menus and related components are now a class.<br><br>
•	You can use most JAXBase table commands to interact with a SQL database and its tables.  Additionally, the SQL class provides you with rich features, allowing you to use the specific syntax for your preferred SQL engine.<br><br>
•	JAXBase is designed to be a true cross platform language.  Windows will be the first targeted operating system with Linux support coming in Version 1.  Operating system specific features are not part of the language, but there will be ways to communicate with the operating system using external add-ons that communicate through one or more JAXBase communication classes.<br><br>
•	When the XBase language was introduced, micro-computers had one floppy disk capable of holding under three hundred Kbytes which meant that every byte was precious and thus abbreviated commands were allowed.  Today, clarity is much more important and JAXBase does not allow unfettered abbreviated commands.  Abbreviations, if allowed, are listed on the page describing the command.<br><br>
•	Proper spacing with commands is required.  Expressions have fewer requirements for proper spacing since A=B.OR.C=D is easily parsed by a computer.<br><br>
•	There is no local database container support.  Further, only simple indexes (IDX) are available.  Local tables are meant for temporary storage or small data needs, though they have the usual 2GB limit.<br><br>
•	There is no report or label preview rendering.  Reports and labels will be rendered using open-source office suites.<br><br>
<BR>
<BR>
<BR>
## Mission Statement
The mission of the JAXBase Project is to encourage use of open-source software and the renewal of the XBase language as a powerful and modern solution, allowing the thousands of legacy XBase applications to once again be relevant to Windows and Linux users.  
<BR>
<BR>

### Goals
- To create and encourage the use and development of secure, open-source software.<br>
- To create an ecosystem that encourages the development of multilingual applications.<br>
- To create a cross-platform language that frees the developer and user from hardware/OS concerns, licensing fees, and use restrictions. <br>
- To allow users to create responsive applications that can run on something as small as the amazing Raspberry PI and have no limits on the hardware to which it can be scaled.<br>
- To encourage further development of the XBase ecosystem, supporting the use of popular back-end SQL database engines, including ones that are not open-source.<br>
- To encourage integration with other open-source projects.<br>
<BR>
<BR>
<BR>

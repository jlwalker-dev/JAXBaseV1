# JAXBaseV1
A modern take on XBase

The JAXBase Project is an attempt to create a modern version of the venerable XBase language. It is highly object oriented and most legacy/MS-DOS commands and last-century paradigms have been removed.  It is expected to become open-source when Version 1.0 is released and be under the GNU General Public License version 2 (GPL-2.0).

## Differences from Most XBase Dialects
•	The @ commands, color pairs, and color schemes no longer exist.<br><br>
•	GUI Form objects will have basic text-only auto-conversion in Version 1.0.<br><br>
•	ON ERROR is gone.  Use TRY/CATCH/FINALLY or the Error method in classes.<br><br>
•	Menus and related components are now a class.<br><br>
•	You can use most JAXBase table commands to interact with a SQL database and its tables.  Additionally, the SQL class provides you with rich features, allowing you to use the specific syntax for your preferred SQL engine.<br><br>
•	JAXBase is designed to be a true cross platform language.  Windows will be the first targeted operating system with Linux support coming in Version 1.  Operating system specific features that are not part of the language, but there will be ways to communicate with the operating system using external add-ons that communicate through one or more JAXBase communication classes.<br><br>
•	When the XBase language was introduced, micro-computers had one floppy disk capable of holding under three hundred Kbytes which meant that every byte was precious and thus abbreviated commands were allowed.  Today, clarity is much more important and JAXBase does not allow abbreviated commands except for a very few instances that are ubiquitous in the industry. <br><br>
•	Proper spacing with commands is required.  Expressions have fewer requirements for proper spacing since A=B.OR.C=D is easily parsed by a computer.<br><br>
•	There is no local database container support.  Further, only simple indexes (IDX) are available.  Local tables are meant for temporary storage or small data needs, though they have the usual 2GB limit.<br><br>
•	There is no report or label preview rendering.  Reports and labels will be rendered using open-source office suites.<br><br>

## Current Status - 9/20/2025
•	The project includes table and index manipulation, the compiler tokenizes most commands, and enough of the runtime exists to do basic testing.<br><br>
•	Development was halted 8/1/2025 to focus attention on the language manual. Development will restart in October.<br><br>
•	The project is currently written in C# and will remain that way through Version 1.<br><br>
•	Version 1 will be considered an educational tool as no serious effort is being put into security concerns.  Version 2 will address those concerns.<br><br>
•	This is Version 0.1 and is an unfinished document release to GitHub which can be considered being a "Request for Comment" period.  Version 0.2 will be a proposed Final Draft, and Version 0.3 will be the Final Draft of Version 1.0 of JAXBase.<br><br>
•	If you see a Status header on a page, then that feature is not expected to be in the version 0.4 release.<br><br>
•	The first public binary release will be Version 0.4 for Windows.  At present, JAXBase is written in C# and is expected to remain in C# through Version 1.<br><br>
•	Form, TextBox, Label, and CommandButton are the only visual classes slated for Version 0.4 of JAXBase.  Most visual classes will be available in Version 0.6, while the rest are expected to show up by the Version 1.0 release.<br><br>
•	The Version 0.4 binary will be released when a simple form containing labels, text boxes, and buttons can be correctly displayed, and a user can interact with the form and save data to a table.<br><br>
•	The code for table and index handling is written, though it is still in testing.  Tables are currently set for exclusive access only.  Version 0.8 will introduce table/record locking and sharing.<br><br>
•	The compiler correctly tokenizes over 80% of the commands while about 20% of the runtime code is written.  GitHub will be updated periodically with progress notes and documentation updates. <br><br>


## Mission Statement
The mission of the JAXBase Project is to encourage use of open-source software and the renewal of the XBase language as a powerful and modern solution, allowing the thousands of legacy XBase applications to once again be relevant to Windows and Linux users.  

### Goals
To create and encourage the use and development of secure, open-source software.<br>
To create an ecosystem that encourages the development of multilingual support.<br>
To create a cross-platform language that frees the developer and user from hardware/OS concerns, licensing fees, and use restrictions. <br>
To allow users to create responsive applications that can run on something as small as the amazing Raspberry PI and have no limits on the hardware to which it can be scaled.<br>
To encourage further development of the XBase ecosystem, supporting the use of popular back-end SQL database engines, including ones that are not open-source.<br>
To encourage integration with other open-source projects.<br>


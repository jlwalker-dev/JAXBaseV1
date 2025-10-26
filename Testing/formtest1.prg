*-----------------------------------------------------------------------------------------------------
* November 1, 2025 - Jon Walker
*
* Version 0.4 Test
* Before you get too excited, let me explain the very limited smoke and mirrors in use.
*
* 1. No READ EVENTS - We don't need it right now as everything is staying in memory when it 
* goes back to the commmand box.  When READ EVENTS is implemented, JAXBase will act more like
* you would expect and anything that goes out of scope will be immediately terminated.
*
* 2. This is just a form with some events.  If it was an actual program, things could get dicy.
*    Again, because READ EVENTS is not yet supported.
*
* 3. This system is being tested as I write test and demo programs and there's not many.  While
*    indexes seem to be working, I didn't promise full support for Version 0.4, so I didn't add 
*    them to the demonstration form.
*
* That's it for the smoke and mirrors!
*-----------------------------------------------------------------------------------------------------
* Version 0.5 will have support for the following
* Release is not yet set, but I'd like to see it out by March 31, 2026
* I don't see Version 0.6 coming out for at least 6 months after that.
*
* I do have a day job after all :)
*-----------------------------------------------------------------------------------------------------
*	WITH/ENDWITH
*	TRY/CATCH/ENDTRY/FINALLY
*	DO WHILE
*	DO UNTIL
*	DO CASE/CASE/OTHERWISE/ENDCASE
*	SCAN/ENDSCAN
*	Images in Command Button
*	TextBox and Shape classes
*	Anything else I can cram in before the end of March
*
* 	I now have a Raspberry Pi 5 and over the next 6 months may decide to see how har
*	it will be to port over to Linux C#.net
*
*	If you want to get involved in the project writing code, testing, or writing example 
*	and utility programs (Form, table, query, and other editors and wizards) then please
*	write to me at jlwalker.dev@gmail.com
*
*	It's a great way to get into JAXBase and get your name onto the GitHub site and into
*	the PDF documents.
*-----------------------------------------------------------------------------------------------------


*--------------------------------------
* Set up the environment
*--------------------------------------
clear all
close all
set talk off
set echo off
set confirm on
set safety off

*--------------------------------------
* Open or create the table
*--------------------------------------
cTable="PhoneBook"

if file(cTable+".dbf")
    * It exists
else
    * Create it
    ? "Creating",cTable
    create table (cTable) (fname c(40), mname c(40), lname c(40), birthdate d, ;
        address1 c(50), address2 c(50), city c(40), state c(2), zip c(10), country c(50))
endif

use (cTable)

if empty(alias())
    *--------------------------------------
    * DEV NOTE: have to start function 
    * statements with an equals sign
    * for now.  May not get around to
    * fixing this until Version 1.0
    *--------------------------------------
    =messagebox("Failed to open "+cTable,0,"Error")
else
    *--------------------------------------
    * Create the form
    *--------------------------------------
    afrm=createobject("form")
    afrm.caption="Test Form for Version 0.4"
    afrm.height=175
    afrm.width=325

    *--------------------------------------
    * Add the record counter
    *--------------------------------------
    albl=createobject("label")
    albl.name="lblRecs"
    albl.autosize=.F.
    albl.alignment=2
    albl.top=2
    albl.left=220
    albl.height=25
    albl.width=100
    albl.caption="Records"
    albl.visible=.T.

    afrm.addobject(albl)

    *--------------------------------------
    * Add the first name controls
    *--------------------------------------
    albl=createobject("label")
    albl.name="lblFirst"
    albl.top=30
    albl.left=15
    albl.height=25
    albl.width=35
    albl.caption="First"
    albl.visible=.T.

    atxt=createobject("textbox")
    atxt.name="txtFirst"
    atxt.top=30
    atxt.left=60
    atxt.height=25
    atxt.width=200
    atxt.enabled=.F.
    atxt.visible=.T.

    afrm.addobject(albl)
    afrm.addobject(atxt)

    *--------------------------------------
    * Add the last name controls
    *--------------------------------------
    albl=createobject("label")
    albl.name="lblLast"
    albl.top=60
    albl.left=15
    albl.height=25
    albl.width=35
    albl.caption="Last"
    albl.visible=.T.

    atxt=createobject("textbox")
    atxt.name="txtLast"
    atxt.top=60
    atxt.left=60
    atxt.height=25
    atxt.width=200
    atxt.enabled=.F.
    atxt.visible=.T.

    afrm.addobject(albl)
    afrm.addobject(atxt)

    *--------------------------------------
    * Add the button row
    *--------------------------------------
    abtn=createobject("commandbutton")
    abtn.name="btnPrev"
    abtn.top=100
    abtn.left=15
    abtn.height=25
    abtn.width=50
    abtn.caption="Prev"
    abtn.writemethod("click","do btnPrev")
    afrm.addobject(abtn)

    abtn=createobject("commandbutton")
    abtn.name="btnNext"
    abtn.top=100
    abtn.left=70
    abtn.height=25
    abtn.width=50
    abtn.caption="Next"
    abtn.writemethod("click","do btnNext")
    afrm.addobject(abtn)

    abtn=createobject("commandbutton")
    abtn.name="btnAdd"
    abtn.top=100
    abtn.left=125
    abtn.height=25
    abtn.width=50
    abtn.caption="Add"
    abtn.writemethod("click","do btnAdd")
    afrm.addobject(abtn)

    abtn=createobject("commandbutton")
    abtn.name="btnEdit"
    abtn.top=100
    abtn.left=180
    abtn.height=25
    abtn.width=50
    abtn.caption="Edit"
    abtn.writemethod("click","do btnEdit")
    afrm.addobject(abtn)

    abtn=createobject("commandbutton")
    abtn.name="btnDel"
    abtn.top=100
    abtn.left=235
    abtn.height=25
    abtn.width=50
    abtn.caption="Del"
    abtn.writemethod("click","do btnDel")
    afrm.addobject(abtn)

    afrm.writemethod("refresh","do formrefresh")

    *--------------------------------------
    * Show the form
    *--------------------------------------
    afrm.show
    do formrefresh
endif


*--------------------------------------
* Previous button action
*--------------------------------------
procedure btnPrev
    ? "btnPrev"

    if bof()
    else
       skip -1
    endif

    afrm.refresh
endproc

*--------------------------------------
* Next button action
*--------------------------------------
procedure btnNext
    ? "btnNext"
    list memory

    if eof()
    else
        skip
    endif

    afrm.refresh
endproc

*--------------------------------------
* Add button action
*--------------------------------------
procedure btnAdd
    ? "btnAdd"

    if afrm.btnAdd.Caption="Save"
        * Save the record and reset the form for navigation
        ? "Saving"
        replace fname with afrm.txtFirst.value,;
                lname with afrm.txtLast.value

	? "Reset command buttons"
        afrm.btnAdd.caption="Add"
        afrm.btnEdit.caption="Edit"
        afrm.txtFirst.enabled=.F.
        afrm.txtLast.enabled=.F.
        afrm.btnPrev.enabled=.T.
        afrm.btnNext.enabled=.T.
        afrm.btnDel.enabled=.T.
    else
        append blank
        afrm.btnAdd.caption="Save"
        afrm.btnEdit.caption="Cancel"
        afrm.txtFirst.enabled=.T.
        afrm.txtLast.enabled=.T.
        afrm.btnPrev.enabled=.F.
        afrm.btnNext.enabled=.F.
        afrm.btnDel.enabled=.F.
        afrm.txtFirst.setfocus
    endif

    afrm.refresh
endproc

*--------------------------------------
* Edit button action
*--------------------------------------
procedure btnEdit
    ? "btnEdit"

    if afrm.btnEdit.Caption="Cancel"
        afrm.txtFirst.enabled=.F.
        afrm.txtLast.enabled=.F.
        afrm.btnAdd.caption="Add"
        afrm.btnEdit.caption="Edit"
        afrm.btnPrev.enabled=.T.
        afrm.btnNext.enabled=.T.
        afrm.btnDel.enabled=.T.
    else
        afrm.btnAdd.caption="Save"
        afrm.btnEdit.caption="Cancel"
        afrm.txtFirst.enabled=.T.
        afrm.txtLast.enabled=.T.
        afrm.btnPrev.enabled=.F.
        afrm.btnNext.enabled=.F.
        afrm.btnDel.enabled=.F.
        afrm.txtFirst.setfocus
    endif

    afrm.refresh
endproc

*--------------------------------------
* Delete button action
*--------------------------------------
procedure btnDel
    ? "btnDel"

    if reccount()>0
        if deleted()
            recall
        else
            delete
        endif
    endif

    afrm.refresh
endproc

*--------------------------------------
* Form refresh code
*--------------------------------------
procedure FormRefresh
    ? "In Refresh"

    afrm.lblRecs.Caption=transform(recno())+" of "+transform(reccount())

    if reccount()>0
        if deleted()
            afrm.btnDel.caption="Recall"
        else
            afrm.btnDel.caption="Del"
        endif

        afrm.txtFirst.value=trim(fname)
        afrm.txtLast.value=trim(lname)
    endif
endproc

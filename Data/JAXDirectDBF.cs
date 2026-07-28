/* ------------------------------------------------------------------------------------------------------
 * This class is the basis of a DBF Table workarea.  It allows one table/view/cursor to be 
 * opened and handles all the data manipulation local data.  Subsequent calls to the
 * USE method will close the current table and open the new one.
 * 
 * ------------------------------------------------------------------------------------------------------
 * HISTORY
 * =======================
 *  2025 -01-21 - JLW
 *      This is the start of my converting the old QuickBASIC system that I wrote in the 
 *      last century.  The plan is to bring forward the FoxBase2 data capabilities and then 
 *      upgrade to as recent of a version that I can find documentation on.  My intent/wish 
 *      is to support all the way up to VFP Version 9 and then add JAX extensions.
 *      
 *      VFP has the ability to buffer records and return row/column changes.  I think that 
 *      may belong in the DataSessions class.  This class should just read/write records 
 *      and deal with indexes.
 *      
 *      The QB system was quite fast, and I have great hopes some of that speed can translate 
 *      into C#.
 *  
 *  2025-02-06 - JLW
 *      I have tied the JAXDirectDBF in with the new DataSession code and tested it out.  
 *      Opened a DBF and read a data record successfully.  I'll be updating the test code to 
 *      also write a new record.  I'll eventually finish the (big) task of creating and 
 *      updating an IDX.
 *      
 *      Then I need to figure out how to share the files and handle locking to write.
 *      I've started that by opening the files in ReadWrite/Shared mode.
 *      I'll also need to check into BinaryWrite to see if I need to rewrite anything as 
 *      C# has ways of writing data that MIGHT be compatible with how data is stored in 
 *      a DBF.
 *      
 *  2025-04-01 - JLW
 *      Have come to an agreement with Vertican and I've used the time to reseach IDX
 *      file creation.  I'm going to tear apart a indexes using the docs they've
 *      supplied.
 *  
 *  2025-04-02 - JLW
 *      I've downloaded a couple of projects from GitHub but the people who work on them 
 *      are obviously geniuses, because they have virtually no comments in the code and 
 *      the variable and method names are AWFUL!  How the hell do they work on that crap?
 *      I've got code to show me how to create CDX files and I'm having one hell of a time
 *      reading it.  I do see a some interesting things, like how it appears they have 
 *      clases for nearly everything.  Deciphering the logic is just crazy hard as I really
 *      don't know C++ and, again, no documentation worth talking about.
 *      
 *      
 *  2025-04-04
 *      Creating an index is coming along and I have the split node mostly logic figured out 
 *      along with keeping everything in order and linking the nodes together   After tearing 
 *      apart a small index VFP created, I've confirmed that VFP does things differently 
 *      during the build, and probably with less code/logic (definitely faster), but I'll 
 *      worry about optimizing later.  I'm trying keep the code simple and readable.  The
 *      biggest thing I saw was that a root node can also be a leaf node.  I'm glad to see
 *      that because I really wanted to do just that and it just confirms I can.  Tearing
 *      apart an index is of limited utility for the most part, it's pretty much along
 *      the lines of everything I remember reading about indexing when I worked on the QB 
 *      code in the last century.  Damn, I wish I had continued that project when there were
 *      engineers and coders that would have had interest.  I could have posted some real 
 *      decent questions on StackOverflow and gotten answers that aren't "Why are you still 
 *      working with VFP?"
 *      
 *      Right now, I only create a Leaf Node Right by splitting the current leaf node and
 *      then linking everything up.  I'm pretty sure I saw evidence that VFP has the ability 
 *      to create an empty node to the left when I tore those cople of indexes apart. I think 
 *      I'm going to not do that. I think doing a split makes more sense because you don't 
 *      know what's coming.  Why create a node left with one record when you may not need it?  
 *      Doing a split and updating the correct leaf could potentially save code, time, and 
 *      space.  Perhaps I'm missing something, but I'm not an MS engineer, so I'll go with 
 *      what I know.  I remember reading about some of this and can recall a bunch of the
 *      concepts, but unfortunately I tossed those books a long time ago (damed idiot).
 *      
 *      Anyway, it removes the need for Add Node Left logic.  That means I just need to add
 *      a Split Index Right which should also handle the Split Root Node right scenario.
 *      Or I could create a new root and make the old root into an index node. Will need to 
 *      think about that.  If I do, I can optimize the Add Leaf Node Right logic a bit.
 *      
 *      If I do that, the root needs to be able to start anywhere in the file, just like the
 *      docs imply.  I'm also going to have to think hard on the free node list. Do I reuse 
 *      nodes or just reindex when I get a certain number of free nodes built up?  If I reuse 
 *      them, it will be dead last, just before I tackle the CDX index types.  Probaly best
 *      if I do, but I'll have to let it cook for a while.
 *      
 *  2025-04-06 - JLW
 *      Looking back at those original indexes I tore apart, I suddenly figured out 
 *      how the index information is being saved for numeric, date, integer, and datetime.
 *      It's done just like in the DBF except backwards and upside down (LOL).
 *      Fortunately, I'm not always and idiot and can sometimes see the obvious.  Also 
 *      read that varchars can be put into an IDX so I threw in a global that is set 
 *      when you want to make an IDX expression that is for a char or varchar field, it 
 *      returns a space filled string equal of the field's full width.  The other data 
 *      types are all fixed at 4 or 8 characters, so nothing to worry about there.
 *      
 *      As for how the non-character data is stored in an index is simplicity in itself.
 *      If you create switch the negative/positive sign, create a byte array the same way
 *      as saving it to the dbf, then you have a value that is very small for the lowest
 *      negative value and very large for the highest allowed positive value.  It's just
 *      taking advantage with how negative and postitive numeric values are created.
 *      
 *      Building the index down the left whenever possible, and filling in the full
 *      branch before moving right seems to be the way to use the least amount of code.
 *      Whenever something splits, it's:
 *      
 *          To the right, ever to the right.  Never to the left, forever to the right.
 *          (Come sing Hosanna, Hosanna...We cool, cool men)
 *      
 *  2025-04-10 - JLW
 *      I now have an IDX creation for string and numeric indexes and VFP9 can use them. 
 *      Whenever it complains, I tear my index apart looking for the missing pointer,
 *      lost record, or zero filled position.  Once it complained about the last
 *      leaf not having a -1 in the right pointer.  No sense of humor at all!
 *      
 *      It's not  creating an index exactly like the VFP indexes I first looked at, but 
 *      it's definitely compatible and I'm pretty happy with the logic.  Sometimes the 
 *      indexes are identical, but not often.  The early tests showed that MS would not 
 *      split the node in half.  I like my logic better as it means it won't create
 *      another node around those leaves until one of them are filled up.
 *
 *      I'll be testing the others in the coming days.  I do not really see a problem 
 *      finishing this off.  I'm sure I'll run into several gotchas, but mostly I just need
 *      to make sure I follow the docs.  
 *      
 *      After that I'll tackle updating and creating new records with open IDX files.
 *      
 *      
 *  2025-04-12
 *      The IDX creation is SLOW.  OMG it's like DBase 2 on an 8088 floppy disk slow!  
 *      I've determined that the problem is all of the disk writes I'm forced to perform.  
 *      I've also realized that I can index 100,000 records of 100 bytes each, in memory, 
 *      for a bit over 10mb.  So I rewrote a new creation code block and do it in memory.  
 *      It's so much faster.  Not VFP fast, but quite respectable.  I'm keeping the code 
 *      I originally wrote because it will update an index one record at a time, which is 
 *      what you need once the index is built and you start performing CRUD operations on
 *      the table.
 *      
 *  2025-04-14 - JLW     
 *      Adding $Changes field to the DBFInfo field list for all tables, views, & cursors 
 *      when they are opened so that they all act the same, except that non-buffered tables 
 *      will not give useful results.  Of course, I have not yet begun the table buffering 
 *      logic.  Buffering will probably come after I attempt to recreate CDX files as I need 
 *      to let that cook in my head for a while, yet.  I don't see any real problem with 
 *      buffering views and cursors, but I need to think through tables before tackling it.
 *      
 *      Again, I wish there was more documentation on DBF internals.
 *      
 *  2025-04-15 - JLW
 *      I AM AN IDIOT!  Just before going asleep last night, I realized that I'm doing more
 *      work than I need to when I do a split.  Right now I'm creating a new node and
 *      dropping the bottom half of the current node into the new node and updating the
 *      key for the old node in the node above.  WHY????  If I drop the top half of
 *      the current node into the new node, then I only need to add a new reference
 *      and not have to change anything else!  stupid, Stupid, STUPID!
 *      
 *  2025-04-17 - JLW
 *      Added final resting point to the IDXCommand so that I know what leaf and leaf record
 *      holds the reference to the current record.  Very usefull in SKIP, and INSERT commands.
 *      
 *      Also created IDXUpdateRecPosPlusOne() to update the index as quickly as possible after
 *      a record INSERT isn't an append to the dbf.
 *      
 *      Also prototyped the DBFPack() routine.  Pretty simple once I put my mind to it.  Again,
 *      the biggest thing is not not read from or write tp the disk for every record.  Grab
 *      100 records, process them in memory, write, rinse, repeat.
 *
 *
 *  2025-04-19 - JLW
 *      Realized I was doing more work than needed.  GOTO is always to the specified physical record
 *      except when TOP or BOTTOM is given, which is the top/bottom of the index if one is active.
 *      Tossed a bunch of code out and happy about that. Walking the nodemap to GOTO the next index
 *      position was interesting, but completely unneeded.  Kept the code in case I want to come up
 *      with an idea for a new command.
 * 
 *      With the excess code pulled, traversing the index is now a lot easier.  Especially for a skip.
 *      I'm also going to rip out the extra properties I added to the index related classes since they
 *      are now unneeded or redundant.  The nodemap will hold pretty much everything I'll need.
 * 
 *  2025-04-25 - JLW
 *      Copied to the JAXBase system
 *  
 *  2025-05-05 - JLW
 *      Early testing shows that double values are not getting passed out of the data table
 *      correctly.  18174.17 will equal 18174.16920123 or similar when passed into a toekn.
 *      The only fix seems to be either using the field information to round the values
 *      or storing the values as a string and converting when I need them.  Going to have
 *      to think this through very carefully.
 *      
 * 2025-07-07 - JLW
 *      Updated the system to only load memo field pointers unless a flag is set to true in
 *      the parameters.  Also set up a memo JAXMemo class that is used to improve handling
 *      of memo fields.  Since the datatable is not directly written to or read from
 *      the dbf, we can replace the datatypes with classes.  I think when I get into the C++
 *      code, that there will definitely be some rethinking of code to improve data handling
 *      and this might be needed to make buffering work (I still have no clue how I'll do that
 *      but I'm sure I'll figure out something when I put my mind to it).
 *      
 * 2025-07-15 - JLW
 *      I'm good enough for now.  Going to debug as I move forward with other parts of the
 *      system.
 *      
 * 2025-08-16 - JLW
 *      I've collected enough information to start working on lock/unlock and shared/exclusive.
 *
 * 2025-09-25 - JLW
 *      Came up with a way to make sure an index belongs to a table.  JAXBase tables will
 *      have a 36 character GUID license and indexes will need to match that license.
 *      
 * ------------------------------------------------------------------------------------------------------
 *  Big things yet to be done
 *      Shared/Exclusive table logic
 *      Table locking
 *      Record locking
 *      Alter table
 *      Null field handlier
 *      Buffering
 *      CDX files
 *      Index file protection enhancements
 *      Rushmore (not bloody likely!)
 *      
 *  --------------------------------------------------------------------------------------
 *  Table header docs taken from VFP Help and some online resources
 *  --------------------------------------------------------------------------------------
 *  Byte        Description
 *  0        	DBF File type:
 *              0x02   FoxBASE
 *              0x03   FoxBASE+/Dbase III plus, no memo
 *              0x10   JAXBase
 *              0x11   JAXBase with Memo Field
 *              0x12   JAXBase with JSON Field
 *              0x20   JAXBase x64
 *              0x11   JAXBase x64 with Memo Field
 *              0x12   JAXBase x64 with JSON Field
 *              0x30   Visual FoxPro
 *              0x31   Visual FoxPro, autoincrement enabled
 *              0x32   Visual FoxPro with field type Varchar or Varbinary
 *              0x43   dBASE IV SQL table files, no memo
 *              0x54   Visual FoxPro 9
 *              0x63   dBASE IV SQL system files, no memo
 *              0x83   FoxBASE+/dBASE III PLUS, with memo
 *              0x8B   dBASE IV with memo
 *              0xCB   dBASE IV SQL table files, with memo
 *              0xE5   HiPer-Six format with SMT memo file
 *              0xF5   FoxPro 2.x (or earlier) with memo
 *              0xFB   FoxBASE
 *
 * --------------------------------------------------------------------------------------
 * 32 bit header
 * --------------------------------------------------------------------------------------
 *  1 -  3      Last update (YYMMDD)
 *  4 -  7 	    Number of records in file
 *  8 -  9 	    Position of first data record
 * 10 - 11 	    Length of one data record, including delete flag
 * 12 - 27 	    Reserved
 * 28 	        Table flags:
 *              0x01   file has a structural .cdx
 *              0x02   file has a Memo field
 *              0x04   file is a database (.dbc)
 *              This byte can contain the sum of any of the above values. For example, 
 *              the value 0x03 indicates the table has a structural .cdx and a Memo field.
 *              
 * 29 	        Code page mark
 * 30 - 31 	    Reserved, contains 0x00
 * 32 - n 	    Field subrecords
 *              The number of fields determines the number of field subrecords. One 
 *              field subrecord exists for each field in the table.
 *              
 * n+1 	        Header record terminator (0x0D)
 *
 *
 * --------------------------------------------------------------------------------------
 * 64 bit header
 * --------------------------------------------------------------------------------------
 *  Byte        Description
 *  0        	DBF File type:
 *              0x11   JAXBase (64 bit addressing)
 *              
 *  1 -  3      Last update (YYMMDD)
 *  4 - 11 	    Number of records in file
 * 12 - 13 	    Position of first data record
 * 14 - 21 	    Length of one data record, including delete flag
 * 21 - 27 	    Reserved
 * 28 	        Table flags:
 *              0x01   file has a structural .cdx
 *              0x02   file has a Memo field
 *              0x04   file is a database (.dbc)
 *              0x08   file has a JSON field
 *              This byte can contain the sum of any of the above values. For 
 *              example, the value 0x03 indicates the table has a structural .cdx 
 *              and a Memo field.
 *              
 * 29 	        Code page mark
 * 30 - 31 	    Reserved, contains 0x00
 * 32 - n 	    Field subrecords
 *              The number of fields determines the number of field subrecords. 
 *              One field subrecord exists for each field in the table.
 *              
 * n+1 	        Header record terminator (0x0D)
 * --------------------------------------------------------------------------------------
 * 
 * n+2 to n+264 Visual Foxpro tables only: 
 *              A 263-byte range that contains the backlink, which is the relative path 
 *              of an associated database (.dbc) file, information. If the first byte 
 *              is 0x00, the file is not associated with a database. Therefore, database 
 *              files always contain 0x00.
 *              
 * --------------------------------------------------------------------------------------
 * 
 * n+2 to n+264 JAXBase tables only:
 * n+2 - n+3    0x00FF - ID for JAX extended header
 * n+4          36 character GUID which JAXBase indexes must match
 * n+40 - n+64  Reserved
 * n+65 - n+264 List of registered indexes delimited with CHAR 13
 *              
 * --------------------------------------------------------------------------------------
 * FIELD RECORD
 * --------------------------------------------------------------------------------------
 *  0 - 10 	    Field name with a maximum of 10 characters. If less than 10, it is padded with null characters (0x00).
 * 11 	        Field type (See FieldInfo class)
 * 12 - 15 	Displacement of field in record
 * 16 	    Length of field (in bytes)
 * 17 	    Number of decimal places
 * 18 	    Field flags:
 *              0x01   System Column (not visible to user)
 *              0x02   Column can store null values
 *              0x04   Binary column (for CHAR and MEMO only)
 *              0x06   (0x02+0x04) When a field is NULL and binary (Integer, Currency, and Character/Memo fields)
 *              0x0C   Column is autoincrementing
 *              
 * 19 - 22 	    Value of autoincrement Next value
 * 23 	        Value of autoincrement Step value
 * 24 - 31 	    Reserved
 * 
 * 
 * 
 * See JAXErrorList.cs for the list of supportted errors
 * 
 * 
 * 
 * CodePage Values - https://www.tek-tips.com/forums/184/faqs/3162
 * -----------------------------------------------------------------------
 *  0x00 - No Codepage Defined
 *  0x01 - US MSDOS
 *  0x02 - Inernational DOS
 *  0x03 - 1252 Windows ANSI
 *  0x04 - Standard Mac
 *  0x64 - 852 Eastern European MS-DOS
 *  0x65 - Russian MS-DOS
 *  0x66 - Nordic MS-DOS
 *  0x67 - Icelandic MS-DOS
 *  0x68 - Czech MS-DOS
 *  0x69 - Polish MS-DOS
 *  0x6A - Greek MS-DOS
 *  0x6B - Turkish MS-DOS
 *  0x78 - Chinese (Hong Kong, Taiwan) Windows
 *  0x79 - Korean Windows
 *  0x7A - Chinese PRC Windows
 *  0x7B - Japanese Windows
 *  0x7C - Thai Windows
 *  0x7D - Hebrew Windows
 *  0x7E - Arabic Windows
 *  0x96 - Russian Windows
 *  0x98 - Greek Mac
 *  0xC8 - Eastern European Windows
 *  0xC9 - Turkish Windows
 *  0xCA - Turkish Windows
 *  0xCB - Greek Windows
 */

using JAXBase.Core;
using JAXBase.Math;
using JAXBase.Utilities;
using JAXBase.XBase;
using System.Collections;
using System.Data;
using System.Text;
using ZXing;

namespace JAXBase.Data
{
    public class JAXDirectDBF : IDisposable
    {
        const uint ZERO_DATE = 1721426; // 1899-12-31
        const bool PRINTDEBUG = true;

        readonly AppClass App;

        /*===================================================================================*
         * These classes are used to describe the table and related indexes and memo file.
         *===================================================================================*/

        public class IDXRecord
        {
            public byte[] Key = [];
            public int RecPos = 0;
            public int Position = 0;
        }

        /*-----------------------------------------------------------------------------------*
         * The node point is used to record a node's properties when traversing the index
         * so we can create a map of the path taken.  This allows us to easily know
         * what nodes need to be updated when something changes.
         * 
         * The first entry in the list is always the root node and the last entry is always 
         * the leaf node where the record was found (or needs to be inserted).
         *-----------------------------------------------------------------------------------*/
        public class IDXNodePoint
        {
            private int attributes = 0;

            public int Attributes
            {
                get { return attributes; }
                set
                {
                    attributes = value;
                    IsIndexNode = value == 0;
                    IsRootNode = (value & 0x01) > 0;
                    IsLeafNode = (value & 0x02) > 0;
                }
            }

            public bool IsIndexNode { get; private set; } = false;
            public bool IsRootNode { get; private set; } = false;
            public bool IsLeafNode { get; private set; } = false;

            public int Position = 0;
            public int NodeRecord = 0;
        }

        /*-----------------------------------------------------------------------------------*
         * This class is used to create an object which returns the results of an index
         * search.  If we're doing "just a search" then the Command will hold 0,1,99, or a
         * negative flag value.  If we're trying to update an index, then this object is
         * created as a first step and returns where/how the index should be updated.
         * 
         * The "should be updated" needs to be double checked in the second step as
         * there are several factors that will play in the final decision that have
         * no real bearing in the first step.  The first step creates the map and
         * returns a command that is relevant, but may not be completely accurate.
         *-----------------------------------------------------------------------------------*/
        public class IDXCommand
        {
            public byte[] Key = [];             // Key of this DBF record
            public int ID = 0;                  // IDX id
            public int Record = 0;              // DBF Record
            public bool FindOnly = false;       // Flag indicating we're only do a search
            public List<IDXNodePoint> NodeMap = [];
            public bool ReverseOrder = false;    // Search in reverse order

            //public int Position = 0;
            //public int NodeRecord = 0;
            //public int LeafNode = -1;   // Leaf current record is located
            //public int LeafRec = -1;    // Leaf postion for current record

            public int Command = 0;             // Result of search/skip command

            // More information at the top of the various code blocks
            // that take care of these commands.
            //  0 = No result
            //  1 = Found exact match at this position and record
            //  2 = Append to this leaf
            //  3 = Insert to this leaf at this leaf record
            //  5 = Split keys to a new leaf to the right or just create
            //  9 = Found a near match (set near on)
            // 21 = Split keys to a new index right, or just create
            // 23 = Create a new index above
            // 31 = Split to a new root right, or just create
            // 99 = Record is not and should not be part of this index 
            // -1 = Record is not and should be part of this index (index is out of date)
            // -99= Error encountered
        }

        /*-----------------------------------------------------------------------------------*
         * The index information is stored in this class object and it will handle
         * both IDX and CDX structures.  
         *-----------------------------------------------------------------------------------*/
        public class IDXInfo
        {
            public bool Descending = false;         // Is descending
            public bool NaturalOrder = true;        // Indicates if the index is it's natural order - TODO
            public bool IsUnique = false;           // Is unique
            public bool IsCandidate = false;        // Candidate key (future)
            public bool HasFor = false;             // Has a for clause
            public bool IsCDX = false;              // True = CDX, false = IDX
            public bool IsCompactIDX = false;       // Compact flag
            public bool IsCompoundIDX = false;      // Multiple indexes (cdx)
            public bool IsRegistered = true;        // Is it registered?

            public int Signature = 1;               // 
            public int MaxKeys = 0;                 // Max keys per node
            public int IDXListPos = 0;              // Position in list
            public int RootNode = 0;                // Position of first root node
            public int FreeNode = 0;                // Next free node
            public int FileLen = 0;                 // File Length
            public int KeyLen = 0;                  // Length of key
            public int Options = 0;                 //

            public string Name = string.Empty;      // File Stem name
            public string FileName = string.Empty;  // FQFN
            public string KeyClause = string.Empty; // Key expression
            public string ForClause = string.Empty; // For expression

            public IDXCommand RecordStatus = new();
            public Stream? IDXStream = null;
            public bool IOLock = false;             // Can't update/search during read/write of dbf

            public byte[] CurrentKey = [];          // Last key read in
        }



        /*-----------------------------------------------------------------------------------*
         * An index node is read into this object by passing the node position.  The
         * attribute related properties are updated, along with node starting position,
         * number of keys, and left/right pointers.
         * 
         * When created, the ID should to be updated if you wish to  tie it to the 
         * correct index record in the DBFInfo.IDX[] list.
         *-----------------------------------------------------------------------------------*/
        public class IDXNode
        {
            public IDXNode()
            {
                // Set the left/right pointers to -1
                buffer = new byte[512];
                byte[] buf = [255, 255, 255, 255, 255, 255, 255, 255];
                Array.Copy(buf, 0, buffer, 4, 8);
            }

            // What kind of node is this?
            private int attributes = 0;
            public int Attributes
            {
                get { return attributes; }
                set
                {
                    attributes = value;
                    IsIndexNode = (value & 0x02) == 0;
                    IsRootNode = (value & 0x01) > 0;
                    IsLeafNode = (value & 0x02) > 0;
                    buffer[0] = (byte)(value & 0x03);
                }
            }

            // Used to identify node type
            public bool IsIndexNode { get; private set; } = false;
            public bool IsRootNode { get; private set; } = false;
            public bool IsLeafNode { get; private set; } = false;

            // number of keys in this node
            private int keys = 0;
            public int Keys
            {
                get { return keys; }
                set
                {
                    keys = value;
                    byte[] k = Long2Bin(keys);
                    byte[] k2 = BitConverter.GetBytes(keys); // ****************************************************************** CHECK THIS
                    Array.Copy(k, 0, buffer, 2, 2);
                }
            }

            // linked list pointers
            private int leftPtr = -1;
            private int rightPtr = -1;
            public int LeftPtr
            {
                get { return leftPtr; }
                set
                {
                    leftPtr = value;
                    byte[] v = Long2Bin(value);
                    byte[] v2 = BitConverter.GetBytes(value); // ****************************************************************** CHECK THIS
                    Array.Copy(v, 0, buffer, 4, 4);
                }
            }
            public int RightPtr
            {
                get { return rightPtr; }
                set
                {
                    rightPtr = value;
                    byte[] v = Long2Bin(value);
                    byte[] v2 = BitConverter.GetBytes(value); // ****************************************************************** CHECK THIS
                    Array.Copy(v, 0, buffer, 8, 4);
                }
            }

            // Position in file
            public int Position = 0;

            // Which IDX[] is this index?
            public int ID = -1;

            // Receive and break out the buffer when applied
            private readonly byte[] data2 = new byte[2];
            private readonly byte[] data4 = new byte[4];

            // This holds the 512 byte node in a buffer
            private byte[] buffer;
            public byte[] Buffer
            {
                get { return buffer; }
                set
                {
                    buffer = value;

                    Array.Copy(value, 0, data2, 0, 2);          // Attributes
                    Attributes = data2[0];

                    Array.Copy(value, 2, data2, 0, 2);          // Keys
                    keys = (int)Bin2Long(data2); //Utilities.CVU(Convert.ToBase64String(data2));

                    Array.Copy(value, 4, data4, 0, 4);          // Left Pointer
                    LeftPtr = BitConverter.ToInt32(Convert.FromBase64String(Convert.ToBase64String(data4)));

                    Array.Copy(value, 8, data4, 0, 4);          // Right Pointer
                    RightPtr = BitConverter.ToInt32(Convert.FromBase64String(Convert.ToBase64String(data4)));
                }
            }


            /*------------------------------------------------------------------------------------------ 
             * Turn 8 bytes into a long - least significatn byte first
             *------------------------------------------------------------------------------------------*/
            // Convert a byte array into a long value
            public long Bin2Long(byte[] binBytes)
            {
                long result = 0;
                for (int i = 0; i < binBytes.Length; i++)
                    result += (long)System.Math.Pow(256L, i) * binBytes[i];

                return result;
            }


            /*------------------------------------------------------------------------------------------ 
             * Convert long to bytes - least significant byte first
             *------------------------------------------------------------------------------------------*/
            public byte[] Long2Bin(long val)
            {
                //string num;
                byte[] bytes = [0, 0, 0, 0, 0, 0, 0, 0];

                if (val < 0)
                    bytes = [255, 255, 255, 255, 255, 255, 255, 255];
                else
                {
                    int i = 0;

                    while (val > 0)
                    {
                        bytes[i++] = (byte)(val % 256L);
                        val /= 256;
                    }
                }

                return bytes;
            }
        }


        /*-----------------------------------------------------------------------------------*
         * This is the object used to allow all access to the dbf and related index and
         * memo files.  Only the current row is directly exposed through this object.
         * 
         *-----------------------------------------------------------------------------------*/
        public class DBFInfo
        {
            public string Alias = string.Empty;
            public string FQFN = string.Empty;
            public string TableName = string.Empty;
            public string TableType = "T";
            public string Connection = string.Empty;
            public string TableRef = string.Empty;      // Holds FQFN or SQL result reference (database!table)

            public bool Buffered = false;
            public int CodePage = 3;
            public string DBCLink = string.Empty;
            public bool DBFEOF = false;
            public bool DBFBOF = false;
            public bool Exclusive = false;
            public int FieldCount = 0;
            public int FileLen = 0;
            public string FilterExpr = "";
            public int FirstPos = 0;
            public bool HasCDX = false;
            public bool HasMemo = false;
            public int HeaderByte = 0;
            public int HeaderLen = 0;
            public int Index = 0;
            public string Indexes = string.Empty;
            public bool IsDBC = false;
            public string LastUpdate = string.Empty;
            public int Modified = 0;
            public bool NoUpdate = false;
            public int RecordLen = 0;

            public List<JAXTables.FieldInfo> Fields = [];
            public int VisibleFields = 0;
            public List<string> FieldData = [];

            public MemoInfo Memo = new();

            public CDXInfo CDX = new();
            public List<IDXInfo> IDX = [];
            public int ControllingIDX = -1;
            public bool CreatingIDXExpression = false;

            public Stream? DBFStream = null;
            public Stream? MemoStream = null;
            public Stream? CDXStream = null;

            public DataTable EmptyRow = new();
            public DataTable CurrentRow = new();
            public DataTable Cursor = new();

            public bool currentRowIsDeleted { get; private set; } = false;

            public void SetDeleted(bool deleted)
            {
                currentRowIsDeleted = deleted;
            }

            public int LogicalRecNo = 0;
            public int RecNo = 0;
            public int RecCount = 0;
            public bool Found = false;
            public ExecutorCodes? LastLocate = null;


            // 2025-09-25 - TODO - JAXBase tag to confirm an Index, memo, and JSON files belong to the DBF
            public string GUID = string.Empty;
            public string SysID = string.Empty;

            public byte[] Buffer = [];


            // Some table types can contain more than one row
            // in memory (views, cursors, and buffered tables)
            // CurrentRecNo tells you which record you are
            // currently pointing at in the table object
            private int currentRecNo = 0;
            public int CurrentRecNo
            {
                get { return currentRecNo; }
                set
                {
                    if (TableType.Equals("T") && Buffered == false)
                    {
                        // Unbuffered tables are treated differently in
                        // that only the current record is in memory
                        currentRecNo = 1;
                    }
                    else
                    {
                        // Views, cursors, and buffered tables are handled here
                        // just make sure we stay in between the lines
                        currentRecNo = value < 1 ? 1 : value > CurrentRow.Rows.Count ? CurrentRow.Rows.Count : value;
                    }
                }
            }
        }


        /*-----------------------------------------------------------------------------------*
         * Holds the memo file information so that the memo file can be easily manipulated
         *-----------------------------------------------------------------------------------*/
        public class MemoInfo
        {
            public ushort BlockSize = 0;
            public int BlockType = 0;
            public int TextStart = 0;
            public int TextLen = 0;
            public int Record = 0;
            public long NextFree = 0;
            public string FileName = string.Empty;
        }


        /*-----------------------------------------------------------------------------------*
         * This class holds the CDX information
         *-----------------------------------------------------------------------------------*/
        public class CDXInfo
        {
            public ushort BlockSize = 0;
            public int BlockType = 0;
            public int Record = 0;
            public int NextFree = 0;
            public string FileName = string.Empty;
        }


        /*-----------------------------------------------------------------------------------*
         * Globals for the class
         *-----------------------------------------------------------------------------------*/
        public DBFInfo DbfInfo { get; private set; } = new();       // This object is the heart of all dbf handling
        private bool InSetup = false;

        /*===================================================================================*
         * Accepts Fully Qualified File Name as input, which if the DBF exists, it will 
         * be opened, plus optionally any memo, or cdx files.  
         * 
         * The public DbfInfo variable contains the information on the table, which can 
         * be read by the calling program.  Making changes to it is a terrible, terrible
         * idea and everything should be done through the public methods.
         * 
         * If no file name is provided, everything is left in the initialization 
         * state, ready to be used by the parent program.
         * 
         * VERY IMPORTANT - A new JAXDirectDBF is created for each database, table, cursor, 
         * or view opened up in a data session.  If you need to access the DBC while you
         * have a table open (such as n the DBCFixFields method), you'll create a temp
         * DirectDBF object and access the DBC directly.
         *===================================================================================*/
        public JAXDirectDBF(AppClass app)
        {
            // Just open up a blank DBF workarea
            App = app;
        }




        /*===================================================================================*
         *===================================================================================*
         * Open a DBC, which is just a DBF with a specific structure.  The user can
         * add fields to the structure, but the minimum required fields must exist.
         *-----------------------------------------------------------------------------------*/
        public async Task<int> DBCOpen(string dbcName)
        {
            try
            {
                await DBFUse(dbcName, string.Empty, false, false, string.Empty);
                if (DbfInfo.IsDBC == false)
                {
                    await DBFClose();
                    throw new Exception("1553|" + dbcName);
                }
            }
            catch (Exception ex)
            {
                // Execution error
                AppErrorHandling.SetError(8005, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return 0;
        }

        /*-----------------------------------------------------------------------------------*
         * A DBC is a DBF and can be closed the same way
         *-----------------------------------------------------------------------------------*/
        public async Task DBCClose()
        {
            await DBFClose();
        }

        /*-----------------------------------------------------------------------------------*
         * Get a list of all tables from the DBC file
         *-----------------------------------------------------------------------------------*/
        public async Task<List<string>> DBCGetTables()
        {
            JAXMath jaxMath = new();
            List<string> dbcFiles = [];

            try
            {
                if (DbfInfo.IsDBC)
                {
                    await DBFGotoRecord("top");
                    DataTable dt = await DBFSelect("*", "all", "objecttype='Table' and not deleted()");

                    foreach (DataRow row in dt.Rows)
                    {
                        GenericClass gc = await jaxMath.SolveMath("trim(objectname)");
                        dbcFiles.Add(gc.Value.Element.ValueAsString);
                    }
                }
                else
                    throw new Exception("1553|" + DbfInfo.FQFN);
            }
            catch (Exception ex)
            {
                // Execution error
                AppErrorHandling.SetError(8005, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return dbcFiles;
        }

        /*-----------------------------------------------------------------------------------*
         * Get the long field names from teh DBC and update the Fields list with
         * those new names.
         *-----------------------------------------------------------------------------------*/
        public async Task<bool> DBCFixFields(DBFInfo dbfinfo)
        {

            try
            {
                JAXDirectDBF table = new(App);
                await DBFUse(dbfinfo.DBCLink, string.Empty, false, true, string.Empty);

                string tableName = DbfInfo.TableName;

                if (table.DbfInfo.IsDBC)
                {
                    // find record where objecttype = "Table" and objectname=tablename
                    DataTable dt = await table.DBFSelect("*", "top 1", string.Format("objecttype='Table' and lower(objectname)=lower('{0}')", tableName));

                    // ObjID= field objectID 
                    if (int.TryParse(dt.Rows[0]["objectid"].ToString(), out int ObjID))
                    {
                        dt = await table.DBFSelect("*", "all", string.Format("objecttype='' and parentid={1}", "", ObjID));

                        // The DBC holds all fields in the order they appear in the table, but
                        // Fields[] holds the $del as Fields[0] and that needs to be skipped.
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            // update fields for this table using DBC records that were retrieved
                            dbfinfo.Fields[i + 1].FieldName = (dt.Rows[i]["objectname"].ToString() ?? dbfinfo.Fields[i + 1].FieldName).Trim();
                            string property = dt.Rows[i]["property"].ToString() ?? "*";

                            if (property.Length > 0)
                            {
                                // Try to break out the properties
                                if (property == "*")
                                    throw new Exception("2091|" + dbfinfo.FQFN);  // It was null or error!
                                else
                                {
                                    /*
                                     * Break out the properties (mostly guess work)
                                     * Properties are delimited with chars 0001
                                     * 
                                     * Here is what each section might represent
                                     *  0 - Unknown
                                     *  1 - Default Value
                                     *  2 - Field validation rule
                                     *  3 - Validation error message
                                     *  4 - Field comment
                                     *  5 - Caption
                                     *  6 - Input Mask
                                     *  7 - Format
                                     */
                                    byte[] bDelim = [0, 0, 0, 1];
                                    string Delim = Encoding.UTF8.GetString(bDelim);
                                    string[] prop = property.Split(Delim);

                                    string temp = prop[1][2..];
                                    temp = temp[..(temp.IndexOf('\0'))];
                                    DbfInfo.Fields[i + 1].DefaultValue = temp;

                                    temp = prop[2][2..];
                                    temp = temp[..(temp.IndexOf('\0'))];
                                    DbfInfo.Fields[i + 1].Valid = temp;

                                    temp = prop[3][2..];
                                    temp = temp[..(temp.IndexOf('\0'))];
                                    DbfInfo.Fields[i + 1].ValidMessage = temp;

                                    temp = prop[4][2..];
                                    temp = temp[..(temp.IndexOf('\0'))];
                                    DbfInfo.Fields[i + 1].Comment = temp;

                                    temp = prop[5][2..];
                                    temp = temp[..(temp.IndexOf('\0'))];
                                    DbfInfo.Fields[i + 1].Caption = temp;

                                    temp = prop[6][2..];
                                    temp = temp[..(temp.IndexOf('\0'))];
                                    DbfInfo.Fields[i + 1].InputMask = temp;

                                    temp = prop[7][2..];
                                    temp = temp[..(temp.IndexOf('\0'))];
                                    DbfInfo.Fields[i + 1].Format = temp;
                                }
                            }
                        }
                    }
                    else
                        throw new Exception("3030|" + tableName);
                }
                else
                    throw new Exception("3032|" + tableName);
            }
            catch (Exception ex)
            {
                // Execution error
                AppErrorHandling.SetError(8005, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return AppErrorHandling.ErrorCount() == 0;
        }


        /*===================================================================================*
         *===================================================================================*
         * Table handling routines
         * 
         * Accept Fully Qualified File Name as input, which if the DBF exists, it will 
         * be opened, plus optionally any memo, or cdx files, returning int 0 if everything
         * opened correctly.  Otherwise clears the DbfInfo of everything except
         * name and error information.
         * 
         * The public DbfInfo variable contains the information on the table, which can 
         * be read by the calling program.
         *-----------------------------------------------------------------------------------*/
        public async Task<int> DBFUse(string fullFileName, string aliasName, bool exclusive, bool noUpdate, string connecton)
        {
            return await DBFUse(fullFileName, aliasName, exclusive, noUpdate, connecton, "T");
        }

        public async Task<int> DBFUse(string fullFileName, string aliasName, bool exclusive, bool noUpdate, string connecton, string type)
        {
            // Start the setup process
            InSetup = true;
            DbfInfo.VisibleFields = 0;
            string FileName = fullFileName.Trim().ToLower();
            bool tableErr = false;

            try
            {
                // Make sure type is valid
                type = "TCV".Contains(type.ToUpper()) ? type.ToUpper() : "T";

                // Open up the file if it exists
                if (File.Exists(FileName))
                {
                    // Open the file and read header
                    byte[] buffer = new byte[1024];
                    string header = string.Empty;
                    int headerEnd = 0;

                    // Break out the header info
                    DbfInfo = new()
                    {
                        Alias = aliasName.Length > 0 ? aliasName : JAXLib.JustStem(fullFileName),
                        Exclusive = exclusive,
                        NoUpdate = noUpdate,
                        TableName = JAXLib.JustStem(fullFileName),
                        FQFN = fullFileName,
                        TableRef = fullFileName,
                        TableType = type,
                        SysID = App.SystemCounter()
                    };

                    // Open up the dbf and grab the first 32 characters of the header
                    int fileLen = 0;

                    try
                    {
                        DbfInfo.DBFStream = new FileStream(FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                        fileLen = (int)DbfInfo.DBFStream.Length;

                        DbfInfo.DBFStream.ReadExactly(buffer, 0, 32);
                    }
                    catch (Exception ex)
                    {
                        // If IO error then we could not open the file
                        AppErrorHandling.SetError(101, string.Format("Cannot open file \"{0}\" - ", FileName) + ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    }

                    if (AppErrorHandling.ErrorCount() == 0)
                    {
                        /*
                         * Header Information
                         *  Bytes       Description
                         * -------      -------------------------------------------------- 
                         *  4 -  7 	    Number of records in file
                         *  8 -  9 	    Position of first data record
                         * 10 - 11 	    Length of one data record, including delete flag
                         */
                        int hdrLen = App.utl.CVU(Convert.ToBase64String(buffer, 8, 2));        // header.substr(8, 2)
                        int hbyte = buffer[0];
                        int rlen = App.utl.CVU(Convert.ToBase64String(buffer, 10, 2));

                        // Get the field count
                        int fldCount;

                        if (JAXLib.InList(hbyte, 16, 17, 18, 32, 33, 34, 48, 49, 50))
                            fldCount = (hdrLen - 263) / 32 - 1;     // VFP & JAX table
                        else
                            fldCount = hdrLen / 32 - 1;             // Non VFP table (no dbf link field)

                        // Record Count
                        int rcount = App.utl.CVI(Convert.ToBase64String(buffer, 4, 4));

                        // Field 28 is a properties field (Has CDX, Has Memo, Is DBC)
                        ushort Fld28 = buffer[28];
                        DbfInfo.HasCDX = (Fld28 & 0x01) > 0;
                        DbfInfo.HasMemo = (Fld28 & 0x02) > 0;
                        DbfInfo.IsDBC = (Fld28 & 0x04) > 0;

                        // Get the full header
                        buffer = new byte[hdrLen];
                        DbfInfo.DBFStream!.Position = 0;
                        DbfInfo.DBFStream.ReadExactly(buffer, 0, hdrLen);

                        // Finish filling in the blanks
                        DbfInfo.LastUpdate = string.Format("{0:00}/{1:00}/{2:00}", (int)buffer[2], (int)buffer[3], (int)buffer[1]);  // YY MM DD format
                        DbfInfo.HeaderByte = hbyte;
                        DbfInfo.HeaderLen = hdrLen;
                        DbfInfo.RecCount = rcount;
                        DbfInfo.RecordLen = rlen;
                        DbfInfo.FieldCount = fldCount;
                        DbfInfo.FileLen = fileLen;
                        DbfInfo.CodePage = buffer[29];

                        // Check header length against calculated
                        if (AppErrorHandling.ErrorCount() == 0 && DbfInfo.FileLen < DbfInfo.FieldCount * 32 + 32)
                        {
                            // Error 3
                            AppErrorHandling.SetError(15, "Not a table - " + DbfInfo.FQFN, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                            tableErr = true;
                        }

                        // Check file length against calculated file length
                        // TODO - allow repair option
                        if (AppErrorHandling.ErrorCount() == 0 && DbfInfo.FileLen < DbfInfo.HeaderLen + DbfInfo.RecordLen * DbfInfo.RecCount)
                        {
                            // Error 4
                            AppErrorHandling.SetError(2091, string.Format("Table \"{0}\" has become corrupted.  The table will need to be repaired before using again.", DbfInfo.FQFN), System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                            tableErr = true;
                        }

                        // Write out some debug
                        AppIO.DebugLog(string.Format(""));
                        AppIO.DebugLog(string.Format("DBF File Name: {0}", DbfInfo.FQFN));
                        AppIO.DebugLog(string.Format("Header Byte  : {0}", DbfInfo.HeaderByte));
                        AppIO.DebugLog(string.Format("Last Update  : {0}", DbfInfo.LastUpdate));
                        AppIO.DebugLog(string.Format("Rec Count    : {0}", DbfInfo.RecCount));
                        AppIO.DebugLog(string.Format("Header Length: {0}", DbfInfo.HeaderLen));
                        AppIO.DebugLog(string.Format("Record Len   : {0}", DbfInfo.RecordLen));
                        AppIO.DebugLog(string.Format("Field Count  : {0}", DbfInfo.FieldCount));
                        AppIO.DebugLog(string.Format("File Length  : {0}", DbfInfo.FileLen));
                        AppIO.DebugLog(string.Format("Has CDX      : {0}", DbfInfo.HasCDX));
                        AppIO.DebugLog(string.Format("Has Memo     : {0}", DbfInfo.HasMemo));
                        AppIO.DebugLog(string.Format("DBC Container: {0}", DbfInfo.IsDBC));
                        AppIO.DebugLog(string.Format(""));

                        if (AppErrorHandling.ErrorCount() == 0)
                        {
                            // Create debug header for field list
                            AppIO.DebugLog(string.Format("Field List"));
                            AppIO.DebugLog(string.Format("Field Name   Type   Len Dec Disp Sys   Bin   Auto  Null  NFld"));
                            AppIO.DebugLog(string.Format("--------------------------------------------------------------"));
                            JAXTables.FieldInfo field = new();
                            DbfInfo.Fields.Add(field);

                            // Load the fields information from the header
                            string[] dbFieldVal = new string[DbfInfo.FieldCount + 1];
                            int nullcount = 0;

                            for (int i = 0; i < DbfInfo.FieldCount; i++)
                            {
                                // Read in a field record
                                int bufferStart = 32 + i * 32;
                                string FName = Encoding.UTF8.GetString(buffer, bufferStart, 32);
                                FName = FName[..FName.IndexOf('\0')];
                                uint Fld18 = buffer[bufferStart + 18];

                                field = new()
                                {
                                    FieldName = FName,
                                    FieldType = Encoding.UTF8.GetString(buffer, bufferStart + 11, 1),
                                    FieldLen = buffer[bufferStart + 16],
                                    FieldDec = buffer[bufferStart + 17],
                                    Displacement = App.utl.CVI(Convert.ToBase64String(buffer, bufferStart + 12, 4)),
                                    SystemColumn = (Fld18 & 0x01) > 0,
                                    NullOK = (Fld18 & 0x02) > 0,
                                    BinaryData = (Fld18 & 0x04) > 0,
                                    AutoIncrement = (Fld18 & 0x08) > 0    // Possibly wrong
                                };

                                // If null capable, increment and store the null field count
                                if (field.NullOK)
                                    field.NullFieldCount = ++nullcount;

                                // Create an empty value for the field
                                field.EmptyValue.Element.Value = field.FieldType switch
                                {
                                    "N" or "F" or "Y" or "B" or "I" => 0,
                                    "D" => DateOnly.MinValue,
                                    "T" => DateTime.MinValue,
                                    _ => string.Empty,
                                };

                                // Is it a visible field (not a system field)
                                // Numeric, Float, currencY, douBle, Integer, Date,
                                // dateTime, Varchar (all types), Character, Memo,
                                // General, Logical, (Q)binary data, (W)blob
                                // That's "BCDFGILNMQTVWY" for the OCD among us
                                if ("NFYBIDTVCMGLQW".Contains(field.FieldType))
                                    DbfInfo.VisibleFields++;

                                // If an AutoIncrement field then get next value and step.
                                if (field.AutoIncrement)
                                {
                                    field.AutoIncNext = App.utl.CVI(Convert.ToBase64String(buffer, bufferStart, 4));
                                    field.AutoIncStep = buffer[bufferStart + 23];
                                }

                                // Add it to the Fields list and print some debug
                                DbfInfo.Fields.Add(field);
                                DbfInfo.FieldData.Add(new string(' ', field.FieldLen));

                                string dbug = string.Format("{0,2}) {1,-12}{2,-5}{3,3} {4,3} {5,4} {6,3} {7,5} {8,5} {9,5} {10,4}",
                                    i + 1, field.FieldName, field.FieldType, field.FieldLen,
                                    field.FieldDec, field.Displacement, field.SystemColumn,
                                    field.BinaryData, field.AutoIncrement, field.NullOK, field.NullFieldCount);
                                AppIO.DebugLog(dbug);
                            }

                            // Add the $Changes system field which is
                            // used by table buffering to indicate what
                            // records and fields have changed
                            field = new()
                            {
                                FieldName = "$Changes",
                                FieldType = "1",
                                FieldLen = DbfInfo.VisibleFields + 1,     // first char is deletion status
                                FieldDec = 0,
                                Displacement = -1,
                                SystemColumn = true,
                                NullOK = false,
                                BinaryData = false,
                                AutoIncrement = false    // Possibly wrong
                            };

                            DbfInfo.Fields.Add(field);
                            DbfInfo.FieldData.Add(new string('0', field.FieldLen));

                            // Get the header terminator and check it
                            headerEnd = 32 + DbfInfo.FieldCount * 32;
                            if (buffer[headerEnd] != 13)
                            {
                                // Missing Terminator mark
                                AppErrorHandling.SetError(2091, string.Format("Table \"{0}\" has become corrupted.  The table will need to be repaired before using again. Missing header terminator.", DbfInfo.FQFN), System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                                tableErr = true;
                            }
                        }

                        if (AppErrorHandling.ErrorCount() == 0)
                        {
                            // Set deletion system field length
                            DbfInfo.Fields[0].FieldName = "$del";
                            DbfInfo.Fields[0].FieldType = "L";
                            DbfInfo.Fields[0].FieldLen = 1;
                            DbfInfo.Fields[0].SystemColumn = true;
                        }

                        // Is this a VFP/JAX table?
                        if (AppErrorHandling.ErrorCount() == 0 && JAXLib.InList(DbfInfo.HeaderByte, 16, 17, 18, 32, 33, 34, 48, 49, 50))
                        {
                            // If VFP/JAX, read in next 263 characters for DBC link
                            int bufferStart = headerEnd + 1;

                            string dbcLink = Encoding.UTF8.GetString(buffer, bufferStart, 263).Trim('\0');

                            if (dbcLink.Length > 0)
                            {
                                if (JAXLib.InList(DbfInfo.HeaderByte, 48, 49, 50))
                                {
                                    // It's a VFP DBC Link
                                    if (File.Exists(dbcLink))
                                    {
                                        // Open up the DBC
                                        JAXDataSession thisSession = App.jaxDataSession[App.CurrentDataSession];
                                        string dbName = JAXLib.JustStem(dbcLink);

                                        DbfInfo.DBCLink = dbcLink;

                                        // Open DBC if not already opened
                                        if (thisSession.IsDBUsed(dbName) == false)
                                            await thisSession.OpenDB(dbcLink);

                                        // Update field information from DBC
                                        if (AppErrorHandling.ErrorCount() == 0)
                                            await thisSession.Databases[dbName].DBCFixFields(DbfInfo);

                                        AppIO.DebugLog(string.Format("DBC Link     : {0}", DbfInfo.DBCLink));
                                    }
                                    else
                                    {
                                        // ERROR - DBC found
                                        AppErrorHandling.SetError(1578, "Invalid database table name: " + dbcLink, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                                        tableErr = true;
                                    }
                                }
                                else
                                {
                                    // TODO - It's a JAXBase sedcondary header
                                    // Make sure it starts with 0x00FF

                                    // Get the GUID tag
                                    DbfInfo.GUID = string.Empty;
                                }
                            }
                        }

                        // Is there a memo (FPT) file?
                        if (AppErrorHandling.ErrorCount() == 0 && DbfInfo.HasMemo)
                        {
                            // Create a memo object and save it
                            MemoInfo dbfMemo = new();

                            // Set up the expected extension for this type of table
                            string ext = JAXLib.JustExt(DbfInfo.FQFN).ToLower() switch
                            {
                                "mnx" => ".mnt",
                                "scx" => ".sct",
                                "vcx" => ".vct",
                                _ => ".fpt"
                            };

                            // Look for the FPT file
                            string memoName = JAXLib.JustFullPath(DbfInfo.FQFN) + JAXLib.JustStem(DbfInfo.FQFN) + ext;

                            try
                            {
                                DbfInfo.MemoStream = new FileStream(memoName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                                dbfMemo.FileName = memoName;

                                // Get Next Free block
                                buffer = new byte[4];
                                DbfInfo.MemoStream.ReadExactly(buffer, 0, 4);   // Bytes 0 - 3
                                dbfMemo.NextFree = App.utl.RevBin2Long(buffer);

                                buffer = new byte[2];
                                DbfInfo.MemoStream.Position = 6;
                                DbfInfo.MemoStream.ReadExactly(buffer, 0, 2);   // Bytes 7 & 8
                                dbfMemo.BlockSize = (ushort)App.utl.RevBin2Long(buffer);
                            }
                            catch (Exception ex)
                            {
                                // Execution Error (likely I/O)
                                AppErrorHandling.SetError(8006, ex.Message + string.Format(" - Opening memo file \"{0}\"", memoName), System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                            }

                            DbfInfo.Memo = dbfMemo;
                            AppIO.DebugLog(string.Format("Memo Filename: {0}", dbfMemo.FileName));
                        }
                    }

                    // Set up an empty row
                    DbfInfo.EmptyRow = await DBFCreateEmptyRecord();

                    // And update the cursor
                    DbfInfo.Cursor = DbfInfo.EmptyRow.Copy();
                    DbfInfo.EmptyRow = DbfInfo.EmptyRow.Copy();

                    // Look for a CDX
                    if (AppErrorHandling.ErrorCount() == 0 && DbfInfo.HasCDX)
                    {
                        // Load the CDX object and save it
                        CDXInfo dbfCDX = new();
                        string cdxName = JAXLib.JustFullPath(DbfInfo.FQFN) + JAXLib.JustStem(DbfInfo.FQFN) + ".cdx";

                        try
                        {
                            DbfInfo.CDXStream = new FileStream(cdxName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                            dbfCDX.FileName = cdxName;

                            buffer = new byte[4];
                            DbfInfo.CDXStream.ReadExactly(buffer, 0, 4);   // Read the header - 0 4 0 0
                            dbfCDX.NextFree = App.utl.CVI(Convert.ToBase64String(buffer));
                        }
                        catch (Exception ex)
                        {
                            // Execution Error (likely I/O)
                            AppErrorHandling.SetError(8007, ex.Message + string.Format(" - Opening structural index file \"{0}\"", cdxName), System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        }

                        DbfInfo.CDX = dbfCDX;
                        AppIO.DebugLog(string.Format("CDX Filename : {0}", dbfCDX.FileName));
                    }

                    AppIO.DebugLog(string.Format("DBF Error    : {0}", tableErr));
                    AppIO.DebugLog(string.Format(""));

                    // Setup is done
                    InSetup = false;
                    DataTable dt = DbfInfo.CurrentRow.Copy();

                    if (AppErrorHandling.ErrorCount() == 0)
                    {
                        DbfInfo.CurrentRow = DbfInfo.EmptyRow.Copy();       // Must contain at least an empty row

                        if (DbfInfo.RecCount > 0)
                        {
                            dt = await DBFGotoRecord("top");                   // Get the first record
                            DbfInfo.CurrentRow = dt.Copy();                 // Populate the CurrentRow

                            // Load the top 50 records to the cursor table 
                            // and reset the RecNo to first record
                            //int GetTop = 50;
                            int i = 0;
                            while (DbfInfo.DBFEOF == false)
                            {
                                i++;

                                // Add to cursor
                                DbfInfo.Cursor.Rows.Add(dt.Rows[0].ItemArray);

                                if (i == 1)
                                {
                                    // First record should be only record
                                    while (DbfInfo.Cursor.Rows.Count > 1)
                                        DbfInfo.Cursor.Rows.RemoveAt(0);
                                }

                                // And go to the next record
                                dt = await DBFSkipRecord(1);
                            }
                        }

                        dt = await DBFGotoRecord("top");                   // Get the first record again
                    }
                }
                else
                {
                    // Did not find the table
                    AppErrorHandling.SetError(1, "File \"{0}\" does not exists: " + FileName, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                }
            }
            catch (Exception ex)
            {
                // Execution error
                AppErrorHandling.SetError(8008, ex.Message + "- Opening table " + FileName, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            // If we failed to open, clean things up
            if (AppErrorHandling.ErrorCount() > 0)
            {
                // Close up any open indexes
                if (DbfInfo.IDX is not null)
                {
                    for (int i = 0; i < DbfInfo.IDX.Count; i++)
                    {
                        if (DbfInfo.IDX[i].IDXStream is not null)
                        {
                            DbfInfo.IDX[i].IDXStream!.Close();
                            DbfInfo.IDX[i].IDXStream = null;
                        }
                    }
                }

                // Close the CDX
                if (DbfInfo.CDXStream is not null)
                {
                    DbfInfo.CDXStream.Close();
                    DbfInfo.CDXStream = null;
                }

                // Close the Memo
                if (DbfInfo.MemoStream is not null)
                {
                    DbfInfo.MemoStream.Close();
                    DbfInfo.MemoStream = null;
                }

                // Close the Table
                if (DbfInfo.DBFStream is not null)
                {
                    DbfInfo.DBFStream.Close();
                    DbfInfo.DBFStream = null;
                }

                DbfInfo.TableName = JAXLib.JustStem(FileName);
                DbfInfo.Alias = aliasName;
                DbfInfo.FQFN = FileName;
            }


            return AppErrorHandling.ErrorCount();
        }

        /*-----------------------------------------------------------------------------------*
         *-----------------------------------------------------------------------------------*/
        public async Task<bool> FieldExists(string field)
        {
            return DbfInfo.Fields.FindIndex(x => x.FieldName.Equals(field.Trim(), StringComparison.OrdinalIgnoreCase)) >= 0;
        }


        /*-----------------------------------------------------------------------------------*
         * Like the xBase replace command, each replace statement writes to the file
         * if the writeNow parameter is true
         * 
         * TODO - need to know when a field changes
         *-----------------------------------------------------------------------------------*/
        public async Task DBFReplaceField(string field, JAXObjects.Token value, bool writeNow)
        {
            int f = DbfInfo.Fields.FindIndex(x => x.FieldName.Equals(field, StringComparison.OrdinalIgnoreCase));

            if (f < 0)
                throw new Exception("Field not found");
            else
            {
                string valType = value.Element.Type;
                string fld = DbfInfo.Fields[f].FieldName;

                switch (DbfInfo.Fields[f].FieldType)
                {
                    case "C":
                    case "V":
                        if (valType.Equals("C"))
                            DbfInfo.CurrentRow.Rows[0][fld] = value.Element.ValueAsString;
                        else
                            throw new Exception("Type mismatch");
                        break;

                    case "B":
                    case "I":
                    case "N":
                    case "Y":
                        if (valType.Equals("N"))
                            DbfInfo.CurrentRow.Rows[0][fld] = value.Element.ValueAsDouble;
                        else
                            throw new Exception("Type mismatch");
                        break;

                    case "L":
                        if (valType.Equals("L"))
                            DbfInfo.CurrentRow.Rows[0][fld] = value.Element.ValueAsBool;
                        else
                            throw new Exception("Type mismatch");
                        break;

                    // *MEMO*   - Need to figure this out
                    case "M":
                    case "W":
                    case "G":
                        if (valType.Equals("C"))
                            DbfInfo.CurrentRow.Rows[0][fld] = value.Element.ValueAsString;
                        else
                            throw new Exception("Type mismatch");
                        break;

                    case "D":
                        if ("TD".Contains(valType, StringComparison.OrdinalIgnoreCase))
                            DbfInfo.CurrentRow.Rows[0][fld] = (DateOnly)value.Element.Value;
                        else
                            throw new Exception("Type mismatch");
                        break;

                    case "T":
                        if ("TD".Contains(valType, StringComparison.OrdinalIgnoreCase))
                            DbfInfo.CurrentRow.Rows[0][fld] = (DateTime)value.Element.Value;
                        else
                            throw new Exception("Type mismatch");
                        break;

                    default:
                        throw new Exception("Unsupported data type " + DbfInfo.Fields[f].FieldType);

                }

                // If it's not a buffered table or the
                // write record now flag is set
                if (DbfInfo.Buffered == false || writeNow)
                {
                    await DBFWriteRecord(DbfInfo.CurrentRow.Rows[0], false);
                    DbfInfo.CurrentRow.Rows[0]["$Changes"] = new string('0', DbfInfo.VisibleFields + 1);
                }
                else
                {
                    // Update the $Changes system field showing which field was updated
                    char[] changes = (DbfInfo.CurrentRow.Rows[0]["$Changes"].ToString() ?? string.Empty).ToCharArray();
                    if (changes.Length >= f)
                    {
                        changes[f] = '1';
                        DbfInfo.CurrentRow.Rows[0]["$Changes"] = new string(changes);
                    }
                }
            }
        }

        /*-----------------------------------------------------------------------------------*
         * Create a model DataTable with one row set to empty value
         *-----------------------------------------------------------------------------------*/
        public async Task<DataTable> DBFCreateEmptyRecord()
        {
            DataTable dt = new();
            object[] values = new object[DbfInfo.Fields.Count];
            string type = string.Empty;

            try
            {
                for (int i = 0; i < DbfInfo.Fields.Count; i++)
                {
                    if (i < 1)
                    {
                        // Deletion column is included, but never seen except in a grid
                        dt.Columns.Add("$del");
                        dt.Columns[i].DataType = System.Type.GetType("System.Boolean");
                        dt.Columns[i].DefaultValue = false;
                    }
                    else
                    {
                        // Only visual columns (after column1) are allowed in the table
                        dt.Columns.Add(DbfInfo.Fields[i].FieldName);

                        dt.Columns[i].DataType = DbfInfo.Fields[i].FieldType switch
                        {
                            "N" => typeof(double),
                            "D" => typeof(DateOnly),
                            "T" => typeof(DateTime),
                            "L" => typeof(bool),
                            "I" => typeof(int),
                            "G" => typeof(JAXTables.JAXMemo),             // Added 2025-07-05 - Setting memo pointers instead of values
                            "M" => typeof(JAXTables.JAXMemo),
                            "W" => typeof(JAXTables.JAXMemo),
                            "F" => typeof(double),
                            "Y" => typeof(double),
                            "B" => typeof(double),
                            _ => typeof(string),
                        };

                        dt.Columns[i].DefaultValue = DbfInfo.Fields[i].FieldType switch
                        {
                            "N" => 0.00D,
                            "D" => DateOnly.MinValue,
                            "T" => DateTime.MinValue,
                            "L" => false,
                            "I" => 0,
                            "G" => new JAXTables.JAXMemo(),
                            "M" => new JAXTables.JAXMemo(),
                            "W" => new JAXTables.JAXMemo(),
                            "F" => 0F,
                            "Y" => 0.00,
                            "B" => 0.00D,
                            _ => string.Empty,
                        };
                    }
                }

                dt.Rows.Add(values);
            }
            catch (Exception ex)
            {
                // Execution error
                AppErrorHandling.SetError(8009, ex.Message + "- Opening table " + DbfInfo.TableName, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return dt;
        }

        /*-----------------------------------------------------------------------------------*
         * Create an empty DBF with optional memo file based on information sent in
         * via a DBFInfo structure.
         *-----------------------------------------------------------------------------------*/
        public async Task<bool> DBFCreateDBF(DBFInfo DbfInfo, bool overWrite)
        {
            // Start the setup process
            bool llSuccess;
            InSetup = true;

            string FileName = DbfInfo.FQFN.Trim().ToLower();

            try
            {
                // Open up the file if it exists
                if (File.Exists(FileName))
                {
                    if (overWrite)
                    {
                        // Delete the file
                        File.Delete(FileName);
                    }
                    else
                        throw new Exception(string.Format("7|File {0} already exists", FileName));
                }

                // Init field/row information
                int recLen = 1;
                int nullCount = 0;
                bool hasMemo = false;

                // Prep the Field List by making sure the
                // lengths are correct for each type
                for (int i = 0; i < DbfInfo.Fields.Count; i++)
                {
                    // Do we have a null field?
                    if (DbfInfo.Fields[i].NullOK)
                    {
                        nullCount++;
                        DbfInfo.Fields[i].NullFieldCount = nullCount;
                    }

                    switch (DbfInfo.Fields[i].FieldType)
                    {
                        case "M":
                        case "W":
                        case "G":
                            DbfInfo.Fields[i].FieldLen = 4;
                            DbfInfo.Fields[i].FieldDec = 0;
                            hasMemo = true;
                            break;

                        case "I":
                            DbfInfo.Fields[i].FieldLen = 4;
                            DbfInfo.Fields[i].FieldDec = 0;
                            break;

                        case "T":
                        case "D":
                            DbfInfo.Fields[i].FieldLen = 8;
                            DbfInfo.Fields[i].FieldDec = 0;
                            break;

                        case "B":
                            DbfInfo.Fields[i].FieldLen = 8;
                            break;

                        case "L":
                            DbfInfo.Fields[i].FieldLen = 1;
                            DbfInfo.Fields[i].FieldDec = 0;
                            break;

                        default:
                            break;
                    }

                    recLen += DbfInfo.Fields[i].FieldLen;
                }

                // Do we need to add a null handler?
                if (nullCount > 0)
                {
                    JAXTables.FieldInfo fld = new()
                    {
                        FieldName = "$null",
                        FieldLen = nullCount / 8 + nullCount % 8 > 0 ? 1 : 0,
                        FieldType = "0",
                        BinaryData = true,
                        SystemColumn = true
                    };

                    DbfInfo.Fields.Add(fld);
                    recLen += fld.FieldLen;
                }

                // Create the header 
                int headerLen = 32 + DbfInfo.Fields.Count * 32 + 264;
                byte[] headerBytes = new byte[headerLen];

                // Create the header byte
                headerBytes[0] = hasMemo ? (byte)0x11 : (byte)0x10; // JAX table with or withoug memo, no cdx

                // Last Update
                headerBytes[1] = (byte)(DateTime.Now.Year - DateTime.Now.Year / 100 * 100);
                headerBytes[2] = (byte)DateTime.Now.Month;
                headerBytes[3] = (byte)DateTime.Now.Day;

                // Number of records in table
                string firstpos = App.utl.MKI(0);
                byte[] fpos = Convert.FromBase64String(firstpos);
                Array.Copy(fpos, 0, headerBytes, 4, 4);

                // Position of first data record (headerlen);
                string rcount = App.utl.MKU((ushort)(headerLen));
                byte[] rc = Convert.FromBase64String(rcount);
                Array.Copy(rc, 0, headerBytes, 8, 2);

                // Length of each data record
                string rlen = App.utl.MKU((ushort)recLen);
                byte[] rl = Convert.FromBase64String(rlen);
                Array.Copy(rl, 0, headerBytes, 10, 2);

                // 12-27 are reserved for ???

                // 28 Table Type Flag
                headerBytes[28] = (byte)((hasMemo ? 0x02 : 0x00) + (DbfInfo.IsDBC ? 0x04 : 0x00));

                // Code page
                headerBytes[29] = (byte)DbfInfo.CodePage;

                // Reserverd
                headerBytes[30] = 0;
                headerBytes[31] = 0;

                // Introduce field records
                int displacement = 1;
                int currentField = 32;

                /*
                 *  1 - 10 	Field name with a maximum of 10 characters. If less than 10, it is padded with null characters (0x00).
                 * 11 	    Field type
                 * 12 - 15 	Displacement of field in record
                 * 16 	    Length of field (in bytes)
                 * 17 	    Number of decimal places
                 * 18 	    Field flags:
                 *              0x01   System Column (not visible to user)
                 *              0x02   Column can store null values
                 *              0x04   Binary column (for CHAR and MEMO only)
                 *              0x06   (0x02+0x04) When a field is NULL and binary (Integer, Currency, and Character/Memo fields)
                 *              0x0C   Column is autoincrementing
                 * 19 - 22 	Value of autoincrement Next value
                 * 23 	    Value of autoincrement Step value
                 * 24 - 31 	Reserved
                 */
                for (int i = 0; i < DbfInfo.Fields.Count; i++)
                {
                    string fieldName = (DbfInfo.Fields[i].FieldName + new string('\0', 10))[..10];
                    string fieldType = (DbfInfo.Fields[i].FieldType.ToUpper())[..1];

                    byte[] fName = Encoding.ASCII.GetBytes(fieldName);
                    byte[] fType = Encoding.ASCII.GetBytes(fieldType);

                    Array.Copy(fName, 0, headerBytes, currentField, 10);
                    headerBytes[currentField + 11] = fType[0];

                    string disp = App.utl.MKI(displacement);
                    byte[] fDisp = Convert.FromBase64String(disp);
                    Array.Copy(fDisp, 0, headerBytes, currentField + 12, 4);

                    headerBytes[currentField + 16] = (byte)DbfInfo.Fields[i].FieldLen;
                    headerBytes[currentField + 17] = (byte)DbfInfo.Fields[i].FieldDec;

                    displacement += DbfInfo.Fields[i].FieldLen;

                    int Fld18 = DbfInfo.Fields[i].SystemColumn ? 1 : 0;
                    Fld18 |= (DbfInfo.Fields[i].NullOK ? 2 : 0);
                    Fld18 |= (DbfInfo.Fields[i].BinaryData ? 4 : 0);
                    Fld18 |= (DbfInfo.Fields[i].AutoIncrement ? 8 : 0);
                    headerBytes[currentField + 18] = (byte)Fld18;
                    string autoincNext = App.utl.MKI(DbfInfo.Fields[i].AutoIncNext);
                    fDisp = Encoding.UTF8.GetBytes(disp);
                    Array.Copy(fDisp, 0, headerBytes, currentField + 19, 4);
                    headerBytes[currentField + 23] = (byte)DbfInfo.Fields[i].AutoIncStep;

                    currentField += 32;
                }

                headerBytes[currentField++] = 13; // Header Termination Mark

                // Finally the JAXBase secondary header
                string jbsh = (new string('\0', 1) + new string((char)255, 1) + Guid.NewGuid() + new string('\0', 263))[..263];
                byte[] jbshbytes = Encoding.UTF8.GetBytes(jbsh);

                // Do you need a memo file?
                if (hasMemo)
                {
                    // Create the memo header
                    byte[] memoHeader = new byte[64];
                    memoHeader[3] = 1;
                    memoHeader[7] = 64;

                    // write the empty file
                    string mfname = JAXLib.JustFullPath(DbfInfo.FQFN) + JAXLib.JustStem(DbfInfo.FQFN) + "." + (DbfInfo.IsDBC ? "DCT" : "FTP");
                    using (var fs = new FileStream(mfname, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        // Write the header bytes to the file and close it up
                        fs.Write(memoHeader, 0, memoHeader.Length);
                    }
                }


                // write the header out to create an empty DBF/DBC file
                using (var fs = new FileStream(FileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    // Write the header bytes to the file and close it up
                    fs.Write(headerBytes, 0, headerBytes.Length);
                    fs.Write(jbshbytes, 0, jbshbytes.Length);
                }

                // Now open it up and populate the DbfInfo
                llSuccess = await DBFUse(FileName, string.Empty, false, false, string.Empty, DbfInfo.TableType) == 0;
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(8010, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return AppErrorHandling.ErrorCount() == 0;
        }


        /*-----------------------------------------------------------------------------------*
         * Close the table, reset DbfInfo
         *-----------------------------------------------------------------------------------*/
        public async Task DBFClose()
        {
            DbfInfo.DBFStream?.Close();
            DbfInfo.CDXStream?.Close();
            DbfInfo.MemoStream?.Close();

            for (int i = 0; i < DbfInfo.IDX.Count; i++)
                DbfInfo.IDX[i].IDXStream?.Close();

            // If it's a cursor then delete the file
            if (DbfInfo.TableType.Equals("C"))
            {
                if (File.Exists(DbfInfo.FQFN))
                    File.Delete(DbfInfo.FQFN);
            }

            DbfInfo = new();
        }


        /*-----------------------------------------------------------------------------------*
         * Append record
         * 
         * Appends a blank record if there is a null table or record.
         * Can append multiple rows.
         * 
         * Send true to DBFWriteRecord to tell it to append a blank record.  The header
         * will be updated appropriately if everything works out ok.
         * 
         * TODO - if more than 1 row being appened, I think it would be faster to
         * convert them to blocks of 100 records and write the blocks.  Just sayin'
         *-----------------------------------------------------------------------------------*/
        public async Task<bool> DBFAppendRecord(DataTable? rows)
        {

            try
            {
                if (rows is null || rows.Rows[0] is null)
                    rows = DbfInfo.EmptyRow;

                for (int i = 0; i < rows.Rows.Count; i++)
                {
                    // TODO - assumes same row structure so let's
                    // make it able to deal with different structures

                    // Update the current row for indexing
                    DbfInfo.CurrentRow.Rows.Add(rows.Rows[i].ItemArray);

                    // Keep only the new row
                    if (DbfInfo.CurrentRow.Rows.Count > 1)
                        DbfInfo.CurrentRow.Rows.RemoveAt(0);

                    // Write the row
                    if ((await DBFWriteRecord(rows.Rows[i], true)) == false)
                        break;

                    if ((i + 1) % 100 == 0)
                        AppIO.DebugLog(string.Format("{0} records appended", i + 1));
                }

                await DBFGotoRecord("bottom");
                AppIO.DebugLog(string.Format("{0} records appended", rows.Rows.Count));
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(8011, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return AppErrorHandling.ErrorCount() == 0;
        }



        /*
         * This does not assume the same structure and tries to make things
         * fit as best as possible.  Fields that don't exist are ignored, fields that
         * are of the wrong type are attempted to be converted.
         * 
         */
        public async Task<bool> DBFAppendForeignRecord(DataTable? rows)
        {

            try
            {
                if (rows is null || rows.Rows[0] is null)
                    rows = DbfInfo.EmptyRow;

                DataTable emptyTable = DbfInfo.EmptyRow.Copy();
                DataTable newTable = DbfInfo.EmptyRow.Copy();
                if (newTable.Rows.Count > 0) newTable.Rows.RemoveAt(0);

                foreach (DataRow row in rows.Rows)
                {
                    DataRow newRow = emptyTable.Rows[0];

                    for (int fld = 0; fld < DbfInfo.Fields.Count; fld++)
                    {
                        string fieldName = DbfInfo.Fields[fld].FieldName;
                        if (rows.Columns.Contains(fieldName))
                        {
                            try
                            {
                                object val = row[fieldName];
                                if (val is DBNull)
                                    newRow[fieldName] = emptyTable.Columns[fieldName]!.DefaultValue;
                                else
                                {
                                    // Try to convert the value to the correct type
                                    Type targetType = emptyTable.Columns[fieldName]!.DataType;
                                    object convertedValue = Convert.ChangeType(val, targetType);
                                    newRow[fieldName] = convertedValue;
                                }
                            }
                            catch
                            {
                                // On error, set to default value
                                newRow[fieldName] = emptyTable.Columns[fieldName]!.DefaultValue;
                            }
                        }
                        else
                        {
                            // Field does not exist in source, set to default
                            newRow[fieldName] = emptyTable.Columns[fieldName]!.DefaultValue;
                        }
                    }

                    newTable.Rows.Add(newRow.ItemArray);
                }

                AppIO.DebugLog(string.Format("{0} records converted", newTable.Rows.Count));
                await DBFAppendRecord(newTable);
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(8011, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return AppErrorHandling.ErrorCount() == 0;
        }

        /*-----------------------------------------------------------------------------------*
         * Insert a record
         *      Backword compatibility with Loc = -1, 0, 1 for before, current, after
         *      
         * Can only insert 1 row at a time, so just accept first row as input
         * TODO - move blocks of 100 rows to make it fast
         *-----------------------------------------------------------------------------------*/
        public async Task<bool> DBFInsertRecord(DataTable? rows, int loc)
        {
            try
            {
                if (rows is null || rows.Rows[0] is null)
                    rows = DbfInfo.EmptyRow;

                int RecNo = DbfInfo.RecNo;

                if (loc == 0)
                {
                    // normal insert at end of table
                    rows ??= DbfInfo.EmptyRow;

                    // If 0, then insert at end of file
                    await DBFAppendRecord(rows);
                }
                else
                {
                    // Insert before and after are backwards compatible commands
                    // If loc>0 then stop before this rec, otherwise move this rec also
                    int RecEnd = (loc > 0) ? DbfInfo.RecNo : DbfInfo.RecNo - 1;

                    // Call the move records routine to append a
                    // blank and move the records down 1 row
                    if (await DBFMoveRecords(loc, RecEnd))
                        await DBFWriteRecord(rows.Rows[0], false);
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(8012, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return AppErrorHandling.ErrorCount() == 0;
        }


        /*-----------------------------------------------------------------------------------*
         * Mark a record as deleted or not - don't care about current state, just marking
         * as requested and then updating indexes
         *-----------------------------------------------------------------------------------*/
        public async Task<bool> DBFDeleteRecord(bool Delete)
        {
            byte[] Data = new byte[1];
            long recPos = DbfInfo.HeaderLen + (DbfInfo.RecNo - 1) * DbfInfo.RecordLen;

            try
            {
                DbfInfo.CurrentRow.Rows[DbfInfo.RecNo][0] = Delete;

                // Update the record with the deletion mark
                Data[0] = (byte)(Delete ? 42 : 32);
                char[] changes = (DbfInfo.CurrentRow.Rows[DbfInfo.RecNo]["$Changes"].ToString() ?? string.Empty).ToCharArray();
                if (changes.Length > 0)
                {
                    changes[0] = '2';
                    DbfInfo.CurrentRow.Rows[DbfInfo.RecNo]["$changes"] = new string(changes);
                }

                if (DbfInfo.DBFStream is not null)
                {
                    DbfInfo.DBFStream.Seek(recPos, SeekOrigin.Begin);
                    DbfInfo.DBFStream.Write(Data, 0, 1);
                    DbfInfo.DBFStream.Flush();

                    // If IDX files are open, update them as they may be looking
                    // at the DELETED() function.
                    if (DbfInfo.IDX.Count > 0)
                    {
                        // Send to idx update routine
                        //llSuccess = IDXWrite();
                    }

                    // If a CDX is open
                    if (DbfInfo.CDXStream is not null)
                    {
                    }
                }
                else
                {
                    throw new Exception("DBF Stream is null for " + DbfInfo.Alias);
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(8013, "Delete/Recalll error - " + ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return AppErrorHandling.ErrorCount() == 0;
        }


        /*-----------------------------------------------------------------------------------*
         * Write the DataTable row to the dbf stream at the location pointed at by 
         * dbfIno.RecNo - only one record is processed and if multiples are sent then
         * an error is thrown.
         *-----------------------------------------------------------------------------------*/
        public async Task<bool> DBFWriteRecord(DataRow row, bool appendRec)
        {
            JAXMath jaxMath = new();
            //StringBuilder Record;
            byte[] Data = new byte[DbfInfo.RecordLen];
            long recPos = DbfInfo.HeaderLen + (DbfInfo.RecNo - 1) * DbfInfo.RecordLen;
            string FieldName = string.Empty;

            try
            {
                if (!appendRec)
                {
                    if (recPos + DbfInfo.RecordLen > DbfInfo.FileLen)
                        throw new Exception("File length vs calculatated length mismatch");
                }

                if (AppErrorHandling.ErrorCount() == 0)
                {
                    for (int i = 0; i <= DbfInfo.FieldCount; i++)
                    {
                        string Field = string.Empty;
                        FieldName = DbfInfo.Fields[i].FieldName;

                        if (i == 0)
                        {
                            Data[0] = (byte)((row[i].ToString() ?? string.Empty).Contains('T') ? 42 : 32);
                        }
                        else
                        {
                            int pos = DbfInfo.Fields[i].Displacement;
                            int len = DbfInfo.Fields[i].FieldLen;
                            byte[] rc;
                            byte[] block;
                            JAXTables.JAXMemo mInfo;

                            switch (DbfInfo.Fields[i].FieldType)
                            {
                                case "Y":   // Currency
                                    double cur = row.Field<double>(DbfInfo.Fields[i].FieldName) * 10000D;
                                    long lon = (long)cur;
                                    Field = App.utl.MKD(lon);
                                    rc = Convert.FromBase64String(Field);
                                    Array.Copy(rc, 0, Data, pos, len);
                                    break;

                                case "D":
                                    if (!DateOnly.TryParse(row[i].ToString(), out DateOnly dto)) dto = new DateOnly(1900, 1, 1);
                                    Field = dto.ToString("yyyyMMdd");
                                    rc = Encoding.ASCII.GetBytes(Field);
                                    Array.Copy(rc, 0, Data, pos, len);
                                    break;

                                case "T":
                                    if (!DateTime.TryParse(row[i].ToString(), out DateTime dtt)) dtt = DateTime.MinValue;
                                    Data = App.utl.DT28Bytes(dtt);
                                    break;

                                case "B":
                                    if (!double.TryParse(row[i].ToString(), out double dbl)) dbl = 0D;
                                    Field = App.utl.MKD(dbl);
                                    rc = Convert.FromBase64String(Field);
                                    Array.Copy(rc, 0, Data, pos, len);
                                    break;

                                case "I":
                                    if (!int.TryParse(row[i].ToString(), out int ival)) ival = 0;
                                    Field = App.utl.MKI(ival);
                                    rc = Convert.FromBase64String(Field);
                                    Array.Copy(rc, 0, Data, pos, len);
                                    break;

                                case "L":
                                    Data[pos] = (byte)((row[i].ToString() ?? string.Empty).Contains('T') ? 84 : 70);
                                    break;

                                // *MEMO*
                                case "M":
                                    mInfo = row.Field<JAXTables.JAXMemo>(DbfInfo.Fields[i].FieldName)!;

                                    if (mInfo.Changed > 1)
                                    {
                                        block = await FPTWriteText(mInfo.Value);

                                        if (App.utl.CVI(Convert.ToBase64String(block)) < 0)
                                            throw new Exception("Failed to write memo field " + DbfInfo.Fields[i].FieldName);
                                        else
                                            Array.Copy(block, 0, Data, pos, len);
                                    }

                                    break;

                                case "F":
                                case "N":
                                    if (!float.TryParse(row[i].ToString(), out float fval)) fval = 0.0F;
                                    string frmt = "###################0";
                                    if (DbfInfo.Fields[i].FieldDec > 0)
                                        frmt += "." + new string('0', DbfInfo.Fields[i].FieldDec);
                                    frmt = JAXLib.Right(frmt, DbfInfo.Fields[i].FieldLen);
                                    Field = fval.ToString(frmt).PadLeft(DbfInfo.Fields[i].FieldLen);
                                    rc = Encoding.ASCII.GetBytes(Field);
                                    Array.Copy(rc, 0, Data, pos, len);
                                    break;

                                case "C":   // Character
                                    Field = row.Field<string>(DbfInfo.Fields[i].FieldName)!.PadRight(DbfInfo.Fields[i].FieldLen);
                                    if (Field.Length > DbfInfo.Fields[i].FieldLen)
                                        throw new Exception("String would be truncated in field " + DbfInfo.Fields[i].FieldName);
                                    else
                                    {
                                        rc = Encoding.ASCII.GetBytes(Field);
                                        Array.Copy(rc, 0, Data, pos, len);
                                    }
                                    break;

                                case "0":   // Null field handler
                                    Field = row.Field<string>(DbfInfo.Fields[i].FieldName)!;
                                    rc = Encoding.ASCII.GetBytes(Field);
                                    Array.Copy(rc, 0, Data, pos, len);
                                    break;

                                case "Q":   // Varbinary
                                case "V":   // Varchar
                                    Field = row.Field<string>(DbfInfo.Fields[i].FieldName)!.PadRight(Field.Length, '\0');
                                    if (Field.Length > DbfInfo.Fields[i].FieldLen)
                                        throw new Exception("String would be truncated in field " + DbfInfo.Fields[i].FieldName);
                                    else
                                    {
                                        if (Field.Length < len) Field += new string('\0', len - Field.Length);
                                        rc = Encoding.ASCII.GetBytes(Field);
                                        Array.Copy(rc, 0, Data, pos, len);
                                    }
                                    break;

                                // *MEMO*
                                case "G":
                                case "W":
                                    mInfo = row.Field<JAXTables.JAXMemo>(DbfInfo.Fields[i].FieldName)!;

                                    if (mInfo.Changed > 1)
                                    {
                                        byte[] block2Write = Encoding.UTF8.GetBytes(mInfo.Value);
                                        block = await FPTWrite(block2Write, true);

                                        if (App.utl.CVI(Convert.ToBase64String(block)) < 0)
                                            throw new Exception("Failed to write blob field " + DbfInfo.Fields[i].FieldName);
                                        else
                                        {
                                            Array.Copy(block, 0, Data, pos, len);
                                        }
                                    }
                                    break;

                                case "1":   // $Changes field doesn't get written
                                    break;
                            }
                        }

                        // Drop out if a problem was found
                        if (AppErrorHandling.ErrorCount() > 0)
                            break;
                    }
                }

                // Write the record
                if (AppErrorHandling.ErrorCount() == 0)
                {
                    // Write the Data array to the file
                    if (DbfInfo.DBFStream is not null)
                    {
                        if (appendRec)
                        {
                            // Prep for append record
                            DbfInfo.FileLen = (int)DbfInfo.DBFStream.Length + DbfInfo.RecordLen;
                            DbfInfo.RecCount++;
                            DbfInfo.RecNo = DbfInfo.RecCount;
                            DbfInfo.CurrentRecNo = DbfInfo.TableType.Equals("T", StringComparison.OrdinalIgnoreCase) ? 1 : DbfInfo.RecCount;
                            recPos = DbfInfo.FileLen;
                            await DBFSetHeader("RC", DbfInfo.RecCount);
                        }
                        else
                        {
                            // Update date of last update
                            await DBFSetHeader("LU", 0);
                        }

                        if (AppErrorHandling.ErrorCount() == 0)
                        {
                            recPos = (DbfInfo.RecNo - 1) * DbfInfo.RecordLen + DbfInfo.HeaderLen;

                            AppIO.DebugLog(string.Format("Writing to dbf record {0} at position {1} length {2}: {3}", DbfInfo.RecNo, recPos, Data.Length, Encoding.UTF8.GetString(Data)));

                            // Write the record
                            DbfInfo.DBFStream.Seek(recPos, SeekOrigin.Begin);
                            DbfInfo.DBFStream.Write(Data, 0, Data.Length);
                            DbfInfo.DBFStream.Flush();

                            // Set the logical, which may change after indexes are updated
                            DbfInfo.LogicalRecNo = DbfInfo.RecNo;
                            DbfInfo.Buffer = Data;
                        }
                    }
                    else
                    {
                        throw new Exception("DBF Stream is null for " + DbfInfo.Alias);
                    }

                    if (AppErrorHandling.ErrorCount() == 0)
                    {
                        // Now update the indexes
                        for (int i = 0; i < DbfInfo.IDX.Count; i++)
                        {
                            GenericClass gc = await jaxMath.SolveMath(DbfInfo.IDX[i].KeyClause);
                            int keyLen = DbfInfo.IDX[i].KeyLen;
                            byte[] keyBytes = await IDXGetKey(gc.Value, keyLen);

                            if (DbfInfo.IDX[i].CurrentKey.Length > 0 && StructuralComparisons.StructuralComparer.Compare(keyBytes, DbfInfo.IDX[i].CurrentKey) != 0)
                            {
                                // There was a change in the key
                                if (DbfInfo.IDX[i].RecordStatus.NodeMap.Count > 0 && DbfInfo.IDX[i].IOLock == false && appendRec == false)
                                {
                                    // Not an append and we have node information
                                    if (DbfInfo.IDX[i].RecordStatus.NodeMap[^1].NodeRecord >= 0)
                                    {
                                        // Remove the record from the leaf node
                                        IDXNode node = await IDXGetNode(DbfInfo.IDX[i].RecordStatus.NodeMap[^1].Position, i);
                                        byte[] noBytes = [];
                                        await IDXUpdateIndexNode(i, DbfInfo.IDX[i].RecordStatus, DbfInfo.IDX[i].RecordStatus.NodeMap.Count - 1, noBytes);
                                    }
                                }

                                // Upate the index with the current record information
                                if (PRINTDEBUG) AppIO.DebugLog(string.Format("Updating index #{0} - {1}", i, DbfInfo.IDX[i].Name));
                                await IDXUpdateIndex(i);

                                // Update the current key
                                DbfInfo.IDX[i].CurrentKey = new byte[keyLen];
                                Array.Copy(keyBytes, 0, DbfInfo.IDX[i].CurrentKey, 0, keyLen);
                            }
                            else
                                AppIO.DebugLog(string.Format("Skipping index {0} update because it is up to date", i));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(8014, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return AppErrorHandling.ErrorCount() == 0;
        }


        /*-----------------------------------------------------------------------------------*
         * Perform a standard record skip, but if an index is in control, skip via
         * the index and not the table.  When done moving to the new record, load it
         * into the DbfInfo and optionally return it.
         * 
         * TODO - *JLW* Add filter and deleted support
         *-----------------------------------------------------------------------------------*/
        public async Task<DataTable> DBFSkipRecord(int skipRecCount, bool loadMemoField = false, bool ignoreFilters = true)
        {
            int currentrec = DbfInfo.RecNo;
            int result = 8015;

            DataTable dt = DbfInfo.EmptyRow.Copy();

            try
            {
                // Force ignore filters if there are no filters set for this dbf
                if (ignoreFilters == false && string.IsNullOrWhiteSpace(DbfInfo.FilterExpr) && Program.CurrentApp.CurrentDS.JaxSettings.Deleted == false)
                    ignoreFilters = true;

                // Is there an index in control of this table?
                if (DbfInfo.ControllingIDX < 0)
                {
                    DbfInfo.RecNo += skipRecCount;
                    DbfInfo.DBFEOF = false;
                    DbfInfo.DBFBOF = false;

                    if (DbfInfo.RecNo < 1)
                    {
                        DbfInfo.CurrentRecNo = 1;
                        DbfInfo.RecNo = 1;
                        DbfInfo.DBFBOF = true;
                    }
                    else if (DbfInfo.RecNo > DbfInfo.RecCount)
                    {
                        DbfInfo.RecNo = DbfInfo.RecCount + 1;
                        DbfInfo.CurrentRecNo = 1;
                        DbfInfo.DBFEOF = true;
                    }

                    if (DbfInfo.RecNo > DbfInfo.RecCount)
                    {
                        // Past EOF so Recno is set reccount +1 and we return an empty row
                        DbfInfo.CurrentRow.Rows.Add(DbfInfo.EmptyRow.Rows[0].ItemArray);
                        while (DbfInfo.CurrentRow.Rows.Count > 1) DbfInfo.CurrentRow.Rows.RemoveAt(0);
                    }
                    else
                    {
                        // Get the row we're pointing at
                        dt = await DBFReadRecord(currentrec != DbfInfo.RecNo, loadMemoField);
                    }
                }
                else
                {
                    // Skip through the index
                    IDXCommand idxCmd = await IDXSkipRecord(skipRecCount, DbfInfo.ControllingIDX);
                    DbfInfo.RecNo = idxCmd.Record;
                    dt = await DBFReadRecord(currentrec != DbfInfo.RecNo, loadMemoField);
                }


                // Do we need to find a different record?
                // Only if skip count is not zero!
                while (skipRecCount != 0)
                {
                    // Have we hit end of file or beginning of file after the move?
                    if ((DbfInfo.DBFEOF && skipRecCount >= 0) || (DbfInfo.DBFBOF && skipRecCount < 0))
                        break;

                    if (ignoreFilters)
                    {
                        // This one works because there are no filters
                        break;
                    }
                    else
                    {
                        // Get the filter expression
                        JAXObjects.Token answer = string.IsNullOrWhiteSpace(DbfInfo.FilterExpr) ? new(true) : await Program.CurrentApp.SolveFromRPNString(DbfInfo.FilterExpr);

                        if (answer.Element.Type.Equals("L"))
                        {
                            if (answer.AsBool())
                            {
                                if (DbfInfo.currentRowIsDeleted == false || Program.CurrentApp.CurrentDS.JaxSettings.Deleted == false)
                                {
                                    // This record will do
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // Error 11
                            result = 11;
                            throw new Exception($"11||Bad filter {DbfInfo.FilterExpr} returns as type {answer.Element.Type}");
                        }
                    }

                    // While the initial Skip count may be greater than on record
                    // in eaither direction, positioning to find the first matching
                    // record will be moving one record at a time until found.
                    if (skipRecCount < 0)
                        await DBFSkipRecord(-1);
                    else
                        await DBFSkipRecord(1);
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(result, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return dt;
        }


        /*-----------------------------------------------------------------------------------*
         * Go to a record in the table
         * 
         * If a string is sent, the value is first checked to see if it's a keyword, and
         * if it is not, it is run through the math processor to get an integer value.
         *
         * If there is a controlling index and "top" or "bottom" was sent as the 
         * expression, grab the appropriate record based on the index, otherwise just 
         * go to the indicated dbf record.
         * 
         * TODO - Add Deleted and Filter support for TOP and BOTTOM
         *-----------------------------------------------------------------------------------*/
        public async Task<DataTable> DBFGotoRecord(string goRecExpr, bool loadMemoFields = false)
        {
            DataTable dt = new();
            int goRec = 0;
            JAXMath jaxMath = new();
            IDXCommand idxCmd = new();

            bool skipDeleted = Program.CurrentApp.CurrentDS.JaxSettings.Deleted;

            // Look for a record position
            try
            {
                switch (goRecExpr.ToLower())
                {
                    case "top":
                        if (DbfInfo.ControllingIDX < 0)
                            goRec = 1;
                        else
                        {
                            // Get the record position of the first indexed record
                            idxCmd = await IDXGoto(true, DbfInfo.ControllingIDX);
                            if (idxCmd.Command == 1) goRec = idxCmd.Record;
                        }

                        // Goto Top does not do anything if there are no records
                        if (DbfInfo.RecCount > 0)
                        {
                            if (goRec > 0 && goRec <= DbfInfo.RecCount + 1)
                                dt = await DBFGotoRecord(goRec, loadMemoFields, false);
                            else
                                throw new Exception(string.Format("Invalid record number {0}", goRec));
                        }
                        break;

                    case "bottom":
                        if (DbfInfo.ControllingIDX < 0)
                            goRec = DbfInfo.RecCount;
                        else
                        {
                            // Get the record position of the last indexed record
                            idxCmd = await IDXGoto(false, DbfInfo.ControllingIDX);
                            if (idxCmd.Command == 1) goRec = idxCmd.Record;
                        }

                        // Goto Bottom does not do anything if there are no records
                        if (DbfInfo.RecCount > 0)
                        {
                            if (goRec > 0 && goRec <= DbfInfo.RecCount + 1)
                                dt = await DBFGotoRecord(goRec, loadMemoFields, false, true);
                            else
                                throw new Exception(string.Format("Invalid record number {0}", goRec));
                        }
                        break;

                    default:
                        GenericClass gc = await jaxMath.SolveMath(goRecExpr);
                        goRec = gc.Value.Element.ValueAsInt;

                        if (goRec > 0 && goRec <= DbfInfo.RecCount + 1)
                            dt = await DBFGotoRecord(goRec, loadMemoFields);
                        else
                            throw new Exception(string.Format("Invalid record number {0}", goRec));
                        break;
                }
            }
            catch (Exception ex)
            {
                goRec = 0;
                AppErrorHandling.SetError(8015, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return dt;
        }



        /*-----------------------------------------------------------------------------------*
         * Go to the record in the table indicated by the first parameter and return a 
         * datatable with 1 row.
         * 
         * Load memo fields if requested by second parameter.
         * 
         * Ignore filters unless instructed by third parameter.
         * 
         * Forth parameter gives direction of travel if filter or deleted refuses record.
         *-----------------------------------------------------------------------------------*/
        public async Task<DataTable> DBFGotoRecord(int goRec, bool loadMemoFields = false, bool ignoreFilters = true, bool movingUp = false)
        {
            DataTable dt = DbfInfo.EmptyRow;
            int result = 8015;

            try
            {
                // Force ignore filters if there are no filters set for this dbf
                if (ignoreFilters == false && string.IsNullOrWhiteSpace(DbfInfo.FilterExpr) && Program.CurrentApp.CurrentDS.JaxSettings.Deleted == false)
                    ignoreFilters = true;

                if (goRec < 1 || goRec > DbfInfo.RecCount)
                {
                    result = 5;
                    throw new Exception($"5||Bad record position {goRec}");
                }
                else
                {
                    dt = await DBFGotoThisRecord(goRec, loadMemoFields);

                    while (true)
                    {
                        if ((DbfInfo.DBFEOF && movingUp == false) || (DbfInfo.DBFBOF && movingUp))
                            break;

                        if (ignoreFilters)
                            break;
                        else
                        {
                            // Get the filter expression
                            JAXObjects.Token answer = string.IsNullOrWhiteSpace(DbfInfo.FilterExpr) ? new(true) : await Program.CurrentApp.SolveFromRPNString(DbfInfo.FilterExpr);

                            if (answer.Element.Type.Equals("L"))
                            {
                                if (answer.AsBool())
                                {
                                    if (DbfInfo.currentRowIsDeleted == false || Program.CurrentApp.CurrentDS.JaxSettings.Deleted == false)
                                    {
                                        // This record will do
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                // Error 11
                                result = 11;
                                throw new Exception($"11||Bad filter {DbfInfo.FilterExpr} returns as type {answer.Element.Type}");
                            }
                        }

                        if (movingUp)
                            await DBFSkipRecord(-1);
                        else
                            await DBFSkipRecord(1);
                    }
                }
            }
            catch (Exception ex)
            {
                DbfInfo.IDX[DbfInfo.ControllingIDX].RecordStatus = new();
                AppErrorHandling.SetError(result, ex.Message, "JAXDirectDBF.DBFGotoRecord");
            }


            // Update the indexes
            if (AppErrorHandling.ErrorCount() == 0 && DbfInfo.IDX.Count > 0)
            {
                JAXMath jaxMath = new();

                // Look at every index and update it's position
                for (int i = 0; i < DbfInfo.IDX.Count; i++)
                {
                    // if not the controlling index
                    if (i != DbfInfo.ControllingIDX)
                    {
                        // if not locked
                        if (DbfInfo.IDX[i].IOLock == false)
                        {
                            bool shouldBeInIDX = true;
                            if (DbfInfo.IDX[i].ForClause.Length > 0)
                            {
                                GenericClass gc = await jaxMath.SolveMath(DbfInfo.IDX[i].ForClause);
                                shouldBeInIDX = gc.Value.TType.Equals("S") && gc.Value.Element.Type.Equals("L") && gc.Value.Element.ValueAsBool;
                            }

                            if (shouldBeInIDX)
                            {
                                string keyClause = DbfInfo.IDX[i].KeyClause;
                                GenericClass gc = await jaxMath.SolveMath(keyClause);
                                await IDXSearch(i, gc.Value.Element.Value, DbfInfo.RecNo, true, true);
                            }
                        }
                    }
                }
            }

            return dt;
        }

        /*-----------------------------------------------------------------------------------*
         * Go to this physical record in the data table and return the row.
         *-----------------------------------------------------------------------------------*/
        private async Task<DataTable> DBFGotoThisRecord(int goRec, bool loadMemoFields = false)
        {
            DataTable dt = new();

            DbfInfo.DBFBOF = false;
            DbfInfo.DBFEOF = false;
            int currentrec = DbfInfo.RecNo;
            int result = 8015;

            try
            {
                if (DbfInfo.RecCount > 0)
                {
                    if (goRec < 1 || goRec > DbfInfo.RecCount + 1)
                    {
                        result = 5;
                        throw new Exception("5||");  // out of reange - throw an error
                    }
                    else
                    {
                        if (goRec > DbfInfo.RecCount)
                        {
                            // goto botom
                            DbfInfo.RecNo = DbfInfo.RecCount + 1;
                            DbfInfo.CurrentRecNo = 1;
                            dt = DbfInfo.EmptyRow.Copy();
                        }
                        else
                        {
                            // Get this record
                            int recPos = DbfInfo.HeaderLen + (goRec - 1) * DbfInfo.RecordLen;
                            DbfInfo.RecNo = goRec;
                            DbfInfo.CurrentRecNo = 1;
                            DbfInfo.LogicalRecNo = goRec;
                            dt = await DBFReadRecord(currentrec != DbfInfo.RecNo, loadMemoFields);
                            DbfInfo.CurrentRow.Rows.Add(dt.Rows[0].ItemArray);

                            if (DbfInfo.CurrentRow.Rows.Count > 1)
                                DbfInfo.CurrentRow.Rows.RemoveAt(0);
                        }
                    }
                }
                else
                {
                    // No records, so return empty row if goRec=1 else error
                    if (goRec == 1)
                    {
                        DbfInfo.RecNo = 1;
                        DbfInfo.CurrentRecNo = 1;
                        DbfInfo.LogicalRecNo = 1;
                        DbfInfo.CurrentRow.Rows.Add(DbfInfo.EmptyRow.Rows[0].ItemArray);
                        DbfInfo.CurrentRow.Rows.RemoveAt(0);
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(result, ex.Message, "JAXDirectDBF.DBFGotoThisRecord");
            }

            return dt;
        }


        /*-----------------------------------------------------------------------------------*
         * Read a physical record (recno) from the DBF and return it in a byte buffer
         * 
         * If the requested record number is not the same as the current recno, or 
         * the refresh flags is set to true, do the actual read, otherwise just
         * pull the buffer from the dbfinfo.buffer.
         * 
         * The routines in this code that call GetRecord must send bool as refresh
         * because the dbfinfo.recno already equals the requested reord.
         * 
         * When the buffer is requested from outside this class is when we usually
         * look at the passed recno vs dbfinfo.recno
         *-----------------------------------------------------------------------------------*/
        private byte[] GetRecord(int RecNo, bool refresh)
        {
            byte[] buffer;

            //if (refresh || RecNo != DbfInfo.RecNo)
            //{
            // Only read the record if not on the current record or forced
            long RecPos = DbfInfo.HeaderLen + (RecNo - 1) * DbfInfo.RecordLen;
            buffer = new byte[DbfInfo.RecordLen];

            if (InSetup || RecNo > DbfInfo.RecCount)
            {
                // If InSetup, we're building the empty row
                for (int i = 0; i < DbfInfo.RecordLen; i++)
                    buffer[i] = 0x20;
            }
            else
            {
                // Read the stream to get the requested row
                DbfInfo.DBFStream!.Position = RecPos;
                DbfInfo.DBFStream.ReadExactly(buffer);
            }
            //}
            //else
            //    buffer = DbfInfo.Buffer;

            return buffer;
        }

        public async Task<JAXObjects.Token> GetFieldValueFromRecordBuffer(byte[] buffer, JAXTables.FieldInfo field)
        {
            JAXObjects.Token result = new();

            int memoPtr;
            string memoText;
            string Field = string.Empty;
            string RecFieldVal = System.Text.Encoding.UTF8.GetString(buffer, field.Displacement, field.FieldLen); // Char, date, datetime, logical
            string RecB64Val = Convert.ToBase64String(buffer, field.Displacement, field.FieldLen);  // all others

            // TODO - take care of nulls here

            // Update the field with a value
            switch (field.FieldType)
            {
                case "Y":   // Currency
                    if (RecFieldVal.Trim().Length == 0)
                        result.Element.Value = 0.0000D;
                    else
                    {
                        // It's basically a double with 4 digits of precision
                        long ldbl = RecFieldVal.Length == 0 ? 0L : App.utl.CVL(RecB64Val);
                        double dbl = ldbl / 10000D;
                        result.Element.Value = dbl;
                    }
                    break;

                case "D":
                    // Date is stored as a yyyyMMdd string
                    if (RecFieldVal.Trim().Length == 0)
                        result.Element.Value = new DateOnly(1900, 1, 1);
                    else
                    {
                        // Convert the value from the field to a date string and then convert to date only
                        RecFieldVal = RecFieldVal[..4] + "-" + RecFieldVal.Substring(4, 2) + "-" + RecFieldVal.Substring(6, 2);
                        if (DateOnly.TryParse(RecFieldVal, out DateOnly dto) == false) dto = new DateOnly(1900, 1, 1);
                        result.Element.Value = dto;
                    }
                    break;

                case "T":
                    if (RecFieldVal.Trim().Length == 0)
                        result.Element.Value = DateTime.MinValue;
                    else
                    {
                        byte[] buffer1 = new byte[8];   // Days since
                        Array.Copy(buffer, field.Displacement, buffer1, 0, 8);
                        result.Element.Value = App.utl.Bytes2DT(buffer1);
                    }
                    break;

                case "B":
                    // Convert the encoded string to a double value
                    if (RecFieldVal.Trim().Length == 0)
                        result.Element.Value = 0.0D;
                    else
                    {
                        double dbl = RecFieldVal.Length == 0 ? 0 : App.utl.CVD(RecB64Val);
                        result.Element.Value = dbl;
                    }
                    break;

                case "I":
                    // Convert the encoded string to an integer
                    if (RecFieldVal.Trim().Length == 0)
                        result.Element.Value = 0;
                    else
                    {
                        int ivl = RecFieldVal.Length == 0 ? 0 : App.utl.CVI(RecB64Val);
                        result.Element.Value = ivl;
                    }
                    break;

                case "L":
                    // Logical is stored as blank/F or T
                    if (RecFieldVal.Trim().Length == 0)
                        result.Element.Value = false;
                    else
                        result.Element.Value = RecFieldVal.Trim().Length > 0 && RecFieldVal.Equals("T", StringComparison.OrdinalIgnoreCase);
                    break;

                case "F":
                case "N":
                    // Convert the encoded string to a single precision number
                    if (RecFieldVal.Trim().Length == 0)
                        result.Element.Value = 0.0;
                    else
                    {
                        if (int.TryParse(RecFieldVal.Trim(), out int nval) == false) nval = 0;
                        result.Element.Value = nval;
                    }
                    break;

                case "C":   // Character
                    if (RecFieldVal.Trim().Length == 0)
                        result.Element.Value = new string(' ', field.FieldLen);
                    else
                        result.Element.Value = RecFieldVal.PadRight(field.FieldLen);
                    break;

                case "0":
                    if (RecFieldVal.Trim().Length == 0)
                        result.Element.Value = new string(' ', field.FieldLen);
                    else
                    {
                        result.Element.Value = BitConverter.ToString(buffer, field.Displacement, field.FieldLen);
                    }
                    break;

                case "Q":   // Varbinary
                case "V":   // Varchar
                    if (RecFieldVal.Contains('\0')) RecFieldVal = RecFieldVal[..RecFieldVal.IndexOf('\0')];
                    result.Element.Value = RecFieldVal;
                    break;

                // *MEMO*
                case "M":   // Memo Field - TODO - better to just load pointer and return value when specifically asked for it
                    if (RecFieldVal.Trim().Length == 0)
                        result.Element.Value = string.Empty;
                    else
                    {
                        memoPtr = App.utl.CVI(RecB64Val);
                        memoText = string.Empty;

                        if (memoPtr > 0)
                            memoText = await FPTReadText(memoPtr);

                        result.Element.Value = memoText;
                    }
                    break;

                // *MEMO*
                case "G":   // General Field
                case "W":   // Blob
                    if (RecFieldVal.Trim().Length == 0)
                        result.Element.Value = string.Empty;
                    else
                    {
                        memoPtr = App.utl.CVI(RecB64Val);
                        memoText = string.Empty;

                        if (memoPtr > 0)
                        {
                            byte[] memoBytes = await FPTRead(memoPtr);
                            result.Element.Value = memoBytes;   // <-- TODO - conversion issues
                        }
                        else
                            result.Element.Value = "";
                    }
                    break;
            }

            return result;
        }


        public async Task<DataTable> DBFReadRecord(bool refresh) { return await DBFReadRecord(refresh, false); }
        public async Task<DataTable> DBFReadRecord(bool refresh, bool loadMemoFields)
        {
            DataTable dt = DbfInfo.EmptyRow.Copy();
            string FieldName = string.Empty;

            try
            {
                if (InSetup == false && (DbfInfo.RecNo > DbfInfo.RecCount || DbfInfo.RecNo < 1))
                    throw new Exception("Record is out of range");
                else
                {
                    DbfInfo.Buffer = GetRecord(DbfInfo.RecNo, refresh);
                    StringBuilder sb = new();

                    // Break it out
                    for (int i = 0; i <= DbfInfo.FieldCount; i++)
                    {
                        FieldName = DbfInfo.Fields[i].FieldName;
                        string Field = string.Empty;
                        string RecFieldVal = System.Text.Encoding.UTF8.GetString(DbfInfo.Buffer, DbfInfo.Fields[i].Displacement, DbfInfo.Fields[i].FieldLen); // Char, date, datetime, logical
                        string RecB64Val = Convert.ToBase64String(DbfInfo.Buffer, DbfInfo.Fields[i].Displacement, DbfInfo.Fields[i].FieldLen);  // all others

                        if (i == 0)
                        {
                            // Deletion Field is * (deleted) or space (not deleted)
                            DbfInfo.SetDeleted(RecFieldVal.Trim().Length > 0);
                            dt.Rows[0][0] = DbfInfo.currentRowIsDeleted;
                        }
                        else
                        {
                            // TODO - take care of nulls here

                            // Update the field with a value
                            switch (DbfInfo.Fields[i].FieldType)
                            {
                                case "Y":   // Currency
                                    if (RecFieldVal.Trim().Length == 0)
                                        dt.Rows[0][i] = 0.0000D;
                                    else
                                    {
                                        // It's basically a double with 4 digits of precision
                                        long ldbl = RecFieldVal.Length == 0 ? 0L : App.utl.CVL(RecB64Val);
                                        double dbl = ldbl / 10000D;
                                        dt.Rows[0][i] = dbl;
                                    }
                                    break;

                                case "D":
                                    // Date is stored as a yyyyMMdd string
                                    if (RecFieldVal.Trim().Length == 0)
                                        dt.Rows[0][i] = new DateOnly(1900, 1, 1);
                                    else
                                    {
                                        // Convert the value from the field to a date string and then convert to date only
                                        RecFieldVal = RecFieldVal[..4] + "-" + RecFieldVal.Substring(4, 2) + "-" + RecFieldVal.Substring(6, 2);
                                        if (DateOnly.TryParse(RecFieldVal, out DateOnly dto) == false) dto = new DateOnly(1900, 1, 1);
                                        dt.Rows[0][i] = dto;
                                    }
                                    break;

                                case "T":
                                    if (RecFieldVal.Trim().Length == 0)
                                        dt.Rows[0][i] = DateTime.MinValue;
                                    else
                                    {
                                        byte[] buffer1 = new byte[8];   // Days since
                                        Array.Copy(DbfInfo.Buffer, DbfInfo.Fields[i].Displacement, buffer1, 0, 8);
                                        dt.Rows[0][i] = App.utl.Bytes2DT(buffer1);
                                    }
                                    break;

                                case "B":
                                    // Convert the encoded string to a double value
                                    if (RecFieldVal.Trim().Length == 0)
                                        dt.Rows[0][i] = 0.0D;
                                    else
                                    {
                                        double dbl = RecFieldVal.Length == 0 ? 0 : App.utl.CVD(RecB64Val);
                                        dt.Rows[0][i] = dbl;
                                    }
                                    break;

                                case "I":
                                    // Convert the encoded string to an integer
                                    if (RecFieldVal.Trim().Length == 0)
                                        dt.Rows[0][i] = 0;
                                    else
                                    {
                                        int ivl = RecFieldVal.Length == 0 ? 0 : App.utl.CVI(RecB64Val);
                                        dt.Rows[0][i] = ivl;
                                    }
                                    break;

                                case "L":
                                    // Logical is stored as blank/F or T
                                    if (RecFieldVal.Trim().Length == 0)
                                        dt.Rows[0][i] = false;
                                    else
                                        dt.Rows[0][i] = RecFieldVal.Trim().Length > 0 && RecFieldVal.Equals("T", StringComparison.OrdinalIgnoreCase);
                                    break;

                                case "F":
                                case "N":
                                    // Convert the encoded string to a double
                                    if (RecFieldVal.Trim().Length == 0)
                                        dt.Rows[0][i] = 0.0D;
                                    else
                                    {
                                        if (double.TryParse(RecFieldVal.Trim(), out double nval) == false) nval = 0;
                                        dt.Rows[0][i] = nval;
                                    }
                                    break;

                                case "C":   // Character
                                    if (RecFieldVal.Trim().Length == 0)
                                        dt.Rows[0][i] = new string(' ', DbfInfo.Fields[i].FieldLen);
                                    else
                                        dt.Rows[0][i] = RecFieldVal.PadRight(DbfInfo.Fields[i].FieldLen);
                                    break;

                                case "0":
                                    if (RecFieldVal.Trim().Length == 0)
                                        dt.Rows[0][i] = new string(' ', DbfInfo.Fields[i].FieldLen);
                                    else
                                    {
                                        dt.Rows[0][i] = BitConverter.ToString(DbfInfo.Buffer, DbfInfo.Fields[i].Displacement, DbfInfo.Fields[i].FieldLen);
                                    }
                                    break;

                                case "Q":   // Varbinary
                                case "V":   // Varchar
                                    if (RecFieldVal.Contains('\0')) RecFieldVal = RecFieldVal[..RecFieldVal.IndexOf('\0')];
                                    dt.Rows[0][i] = RecFieldVal;
                                    break;

                                // *MEMO*
                                case "G":   // General Field
                                case "M":   // Memo Field - TODO - better to just load pointer and return value when specifically asked for it
                                case "W":   // Blob

                                    JAXTables.JAXMemo mInfo = new JAXTables.JAXMemo();

                                    // Load the pointer
                                    if (RecFieldVal.Trim().Length > 0)
                                        mInfo.Pointer = App.utl.CVI(RecB64Val);

                                    // Do we load the memo field also?
                                    if (loadMemoFields && mInfo.Pointer > 0)
                                    {
                                        mInfo.Value = await FPTReadText(mInfo.Pointer);
                                        mInfo.Changed = 2;
                                    }

                                    dt.Rows[0][i] = mInfo;
                                    break;
                            }
                        }

                        sb.Append(string.Format("{0}", dt.Rows[0][i]) + " ");
                    }

                    // Update the current row
                    dt.Rows[0]["$changes"] = new string('0', DbfInfo.VisibleFields + 1);
                    DbfInfo.CurrentRow = dt.Copy();

                    AppIO.DebugLog(string.Format("Row {0}: {1}", DbfInfo.RecNo, sb));
                }
            }
            catch (Exception ex)
            {
                AppIO.DebugLog(string.Format("Error reading record {0} - Field {1}", DbfInfo.RecNo, FieldName));
                AppErrorHandling.SetError(8015, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return dt;
        }


        /*-----------------------------------------------------------------------------------*
         * Emulates the select where you give it a list of fields/expressions and
         * a where/for condition and get back any matches from the table.
         * 
         * Returns: ExitStatus - 0=failure, 1=success, -1=user hit escape
         * 
         * 2025-04-29 - Have no ideas on Rushmore, yet, so this is 100% brute force.
         *              Also, not supporting expressions at this time.  That's for later
         *              when I've gotten this class tied into the DataSessions class.
         *
         * 2025-05-09 - I've come up with some speed enhancements that I need to work out.
         * 
         *-----------------------------------------------------------------------------------*/
        public async Task<DataTable> DBFSelect(string fields, string scope, string whereFor) { return await DBFSelect(fields, scope, whereFor, false); }
        public async Task<DataTable> DBFSelect(string fields, string scope, string whereFor, bool loadMemoFields)
        {
            JAXMath jaxMath = new();
            JAXTables.JAXMemo mInfo;
            int CurrentRow = DbfInfo.CurrentRecNo;
            string tablePart = DbfInfo.Alias + ".";
            DataTable dt = new();
            GenericClass gc;

            try
            {
                List<string> FieldExpression = [];

                // Break out fields individualy looking for " AS " to tell 
                // us that there is an "expression" as "fieldname" and save the
                // expression to the FieldExpr array
                int FldExpr = 0;
                string brkFields = fields;
                int StartRow = 1;
                int ScopeCount = DbfInfo.RecCount;

                string fieldName;
                string fieldExpr;


                // -----------------------------------------------------------------
                // Set up the scope
                // TOP, NEXT, ALL, REST
                // -----------------------------------------------------------------
                if (scope.Length == 0 || scope.Equals("all", StringComparison.OrdinalIgnoreCase))   // ALL
                {
                    StartRow = 1;
                    ScopeCount = DbfInfo.RecCount;
                }
                else if (scope.Equals("rest", StringComparison.OrdinalIgnoreCase))                   // REST
                {
                    StartRow = DbfInfo.RecNo;
                    ScopeCount = DbfInfo.RecCount - StartRow + 1;
                }
                else                                                                                // NEXT, TOP
                {
                    string scopeMath = "0";

                    if (scope[..3].Equals("top", StringComparison.OrdinalIgnoreCase))
                    {
                        StartRow = 1;
                        scopeMath = scope[3..].Trim();
                    }
                    else if (scope[..3].Equals("next", StringComparison.OrdinalIgnoreCase))
                    {
                        StartRow = DbfInfo.RecNo;
                        scopeMath = scope[4..].Trim();
                    }

                    gc = await jaxMath.SolveMath(scopeMath);

                    ScopeCount = 0;
                    if (gc.Value.Element.Type.Equals("N"))
                        ScopeCount = gc.Value.Element.ValueAsInt;

                    if (ScopeCount < 1)
                        throw new Exception("1866|scope");
                }


                // -----------------------------------------------------------------
                // Build the results table
                // -----------------------------------------------------------------
                if (fields.Equals("*"))
                {
                    dt = DbfInfo.EmptyRow.Copy();
                    dt.Rows.RemoveAt(0);
                }
                else
                {
                    while (brkFields.Length > 0)
                    {
                        // Get the leading field expression
                        brkFields = App.utl.GetNextExpr(brkFields, ",", out string Expr);

                        // is there an " as "?
                        if (Expr.Contains(" as ", StringComparison.OrdinalIgnoreCase))
                        {
                            // break it apart
                            int k = Expr.IndexOf(" as ", StringComparison.OrdinalIgnoreCase);
                            string leftExpr = Expr[..k].Trim();
                            string rightField = Expr[(k + 4)..].Trim();

                            dt.Columns.Add(rightField);
                            fieldName = rightField;
                            fieldExpr = leftExpr;
                        }
                        else
                        {
                            // is this a field name?
                            gc = await jaxMath.SolveMath(tablePart + Expr);
                            if (gc.Value.Element.Type.Equals("U"))
                            {
                                // put Exp_x name into FieldsToSave
                                // put expression into FieldExpr
                                fieldName = string.Format("Expr_{0}", ++FldExpr);
                                fieldExpr = Expr;
                            }
                            else
                            {
                                // put field into FieldsToSave
                                // put table.field into FieldExpr
                                fieldName = Expr;

                                //fieldExpr = tablePart + Expr; // TODO - until math parser is fixed
                                fieldExpr = Expr;
                            }
                        }

                        // Add the column name/type to the data table
                        // TODO - we'll need to figure out width.dec also
                        dt.Columns.Add(fieldName);
                        gc = await jaxMath.SolveMath(fieldExpr);
                        var fldType = JAXMathAux.GetTokenDataType(gc.Value);
                        dt.Columns[^1].DataType = fldType;
                        FieldExpression.Add(fieldExpr);
                    }
                }


                // -----------------------------------------------------------------
                // Now go through the table and collect what the user requested
                // No rushmore logic, so have at it!
                // -----------------------------------------------------------------
                bool SkipDeleted = App.CurrentDS.JaxSettings.Deleted;
                int thisRow = StartRow;

                // In case there's an index in control
                await DBFGotoRecord("top", true);
                DataTable tr = await DBFSkipRecord(thisRow - 1, true);

                // Skip any deleted records at start of scope
                string test = tr.Rows[0][0].ToString() ?? "false";
                while (test.Equals("true", StringComparison.OrdinalIgnoreCase) && thisRow < DbfInfo.RecCount)
                {
                    thisRow++;
                    tr = await DBFSkipRecord(1);
                    test = tr.Rows[0][0].ToString() ?? "false";
                }


                // Break the where clause into an RPN list to 
                // speed up the math processing
                List<string> whereRPN = [];
                if (whereFor.Length > 0)
                    whereRPN = jaxMath.ReturnRPN(whereFor);

                while (thisRow < StartRow + ScopeCount && thisRow <= DbfInfo.RecCount && DbfInfo.DBFEOF == false)
                {
                    if (whereRPN.Count < 1 || (await jaxMath.MathSolve(whereRPN)).AsBool())
                    {
                        // No where clause or it passed the test, so this record
                        // will be added to the results
                        if (fields.Equals("*"))
                        {
                            // We're just copying all the fields
                            dt.Rows.Add(tr.Rows[0].ItemArray);
                        }
                        else
                        {
                            // We're copying a subset of the rows with the listed columns/expressions
                            dt.Rows.Add();

                            for (int j = 0; j < dt.Columns.Count; j++)
                            {
                                gc = await jaxMath.SolveMath(FieldExpression[j]);

                                if (gc.Value.Element.Type == "O")
                                {
                                    mInfo = (JAXTables.JAXMemo)gc.Value.Element.Value;
                                    dt.Rows[^1][j] = mInfo;
                                }
                                else
                                    dt.Rows[^1][j] = gc.Value.Element.Value;
                            }
                        }

                        /*
                        // TODO - If user can interrupt, then look for the ESC key
                        int key = JAXLanguage.InKey(1);

                        // TODO - Update system status field - x records found out of y (z% complete)
                        if (key == 27 || thisRow - StartRow % 100 == 0 || dt.Rows.Count % 100 == 0)
                        {
                            string status = string.Format("{0} records - {1}% complete", dt.Rows.Count, dt.Rows.Count * 100 / ScopeCount);
                            Console.WriteLine(status);
                        }

                        if (key == 27)
                        {
                            ExitStatus = -1;
                            break;
                        }
                        */

                        thisRow++;
                    }

                    tr = await DBFSkipRecord(1, true);

                    // skip if deleted
                    test = tr.Rows[0][0].ToString() ?? "false";
                    while (test.Equals("true", StringComparison.OrdinalIgnoreCase) && thisRow < DbfInfo.RecCount)
                    {
                        thisRow++;
                        tr = await DBFSkipRecord(1);
                        test = tr.Rows[0][0].ToString() ?? "false";
                    }
                }

                // Get back to the original row
                await DBFGotoThisRecord(CurrentRow);
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(8015, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return dt;
        }

        /*-----------------------------------------------------------------------------------*
         * Here is the absolutely vital pack logic.  This is the only way to clean up
         * a database, it's indexes, and memo file.
         *----------------------------------------------------------------------------------*/
        public async Task DBFPack()
        {
            try
            {
                if ("TC".Contains(DbfInfo.TableType) && DbfInfo.NoUpdate == false)
                {
                    int headerLen = DbfInfo.HeaderLen;

                    List<JAXTables.FieldInfo> memoFieldList = [];
                    List<JAXTables.FieldInfo> fieldList = [];
                    List<IDXInfo> indexList = [];

                    // Grab info on the current dbf so we can open the new
                    // one in the exact same manner.
                    int codePage = DbfInfo.CodePage;
                    int headerByte = DbfInfo.HeaderByte;
                    int recLen = DbfInfo.RecordLen;
                    int recCount = DbfInfo.RecCount;
                    int controllingIDX = DbfInfo.ControllingIDX;

                    bool buffered = DbfInfo.Buffered;
                    bool exclusive = DbfInfo.Exclusive;
                    bool hasMemo = DbfInfo.HasMemo;
                    bool hasCDX = DbfInfo.HasCDX;
                    bool isDBC = DbfInfo.IsDBC;
                    bool noUpdate = DbfInfo.NoUpdate;

                    string alias = DbfInfo.Alias;
                    string dbcLink = DbfInfo.DBCLink;
                    string fileName = DbfInfo.FQFN;
                    string connection = DbfInfo.Connection;

                    // Set up the expected extension for this type of table
                    string ext = JAXLib.JustExt(DbfInfo.FQFN).ToLower() switch
                    {
                        "mnx" => ".mnt",
                        "scx" => ".sct",
                        "vcx" => ".vct",
                        _ => ".fpt"
                    };

                    // ---------------------------------------------------
                    // Copy the field information
                    // ---------------------------------------------------
                    foreach (JAXTables.FieldInfo fld in DbfInfo.Fields)
                    {
                        // Copy each field
                        fieldList.Add(fld);

                        // add memo, general, and blob types to the memoFieldList
                        if ("MGW".Contains(fld.FieldType))
                            memoFieldList.Add(fld);

                        DbfInfo.Fields.Add(fld);
                    }

                    // Copy each index reference
                    for (int i = 0; i < DbfInfo.IDX.Count; i++)
                        indexList.Add(DbfInfo.IDX[i]);

                    // ---------------------------------------------------
                    // Close the current DBF and open it exclusively
                    // ---------------------------------------------------
                    await DBFClose();
                    await DBFUse(fileName, "_$sourcepack", true, false, string.Empty);

                    // ---------------------------------------------------
                    // Create a new temp DBF
                    // ---------------------------------------------------
                    UtilitiesLib ut = new(App);
                    DateTime dtn = DateTime.Now;
                    long dtd = (dtn.Year % 2000) * 10000 + dtn.Month * 100 + dtn.Day;
                    long dtt = dtd * 10000 + dtn.Hour * 10000 + dtn.Minute + dtn.Second;
                    long dtm = dtd + 1000 + dtn.Millisecond;
                    ut.Conv36(dtd, 4, out string tfn1);
                    ut.Conv36(dtt, 4, out string tfn2);
                    ut.Conv36(dtm, 2, out string tfn3);
                    string dts = JAXLib.JustFullPath(fileName) + tfn1 + tfn2 + tfn3;

                    DBFInfo dbfinfo = new()
                    {
                        CodePage = DbfInfo.CodePage,
                        FQFN = dts
                    };

                    await DBFCreateDBF(dbfinfo, true);

                    JAXDirectDBF tmpDBF = new(App);
                    await tmpDBF.DBFUse(fileName, "_$targetpack", true, false, string.Empty);

                    // ---------------------------------------------------
                    // Starting at record 1 in the current DBF, proceed
                    // to read blocks of 100 records into memory, break
                    // out each record, update any memo info as it's
                    // written to the new memo file, and save the block
                    // ---------------------------------------------------
                    int recs = DbfInfo.RecCount;
                    int targetRec = 0;
                    int sourceRec = 0;
                    int sourcePos = headerLen;
                    int targetPos = headerLen;

                    tmpDBF.DbfInfo.DBFStream!.Position = targetPos;

                    if (headerLen != dbfinfo.HeaderLen)
                        throw new Exception("Pack target header length error");

                    if (DbfInfo.RecordLen != dbfinfo.RecordLen)
                        throw new Exception("Pack target record length error");

                    while (recs > 0)
                    {
                        // grab 100 (or remaining if less than 100) records at a time to the
                        // oldBuffer array and iterated through it
                        int recs2Parse = recs >= 100 ? 100 : recs;
                        recs -= recs2Parse;
                        targetRec = 0;

                        byte[] targetBuffer = new byte[recLen * 100];
                        byte[] sourceBuffer = new byte[recs2Parse * recLen];
                        DbfInfo.DBFStream!.Position = sourcePos;
                        DbfInfo.DBFStream!.ReadExactly(sourceBuffer);
                        byte[] recBytes = new byte[recLen];

                        for (int i = 0; i < recs2Parse; i++)
                        {
                            // Look at the first byte of each record and, if
                            // it's blank, copy it to the target buffer
                            if (sourceBuffer[sourcePos + (sourceRec + i) * recLen] == 32)
                            {
                                Array.Copy(sourceBuffer, sourcePos + i * recLen, recBytes, 0, recLen);

                                if (DbfInfo.HasMemo)
                                {
                                    for (int j = 0; j < memoFieldList.Count; j++)
                                    {
                                        // Get the pointer for this field
                                        byte[] ptrBytes = new byte[4];
                                        Array.Copy(recBytes, memoFieldList[j].Displacement, ptrBytes, 0, 4);
                                        int ptr = App.utl.RevBin2Int(ptrBytes);

                                        // Read the data from this memo file
                                        byte[] mbuffer = await FPTRead(ptr);

                                        // Write to the data to the new memo file
                                        ptrBytes = await tmpDBF.FPTWrite(mbuffer, "GW".Contains(memoFieldList[j].FieldType));

                                        // Update the record pointer
                                        Array.Copy(ptrBytes, 0, recBytes, memoFieldList[j].Displacement, 4);
                                    }
                                }

                                // not marked for deletion, so copy it over and
                                // increment the target record count
                                Array.Copy(recBytes, 0, targetBuffer, targetRec * recLen, recLen);
                                targetRec++;
                            }
                        }

                        // Did we get anything from the block of data?
                        if (targetRec > 0)
                        {
                            // when we're done with that block, write the data we copied
                            // to the end of the temp DBF file
                            tmpDBF.DbfInfo.DBFStream!.Position = targetPos;
                            tmpDBF.DbfInfo.DBFStream!.Write(targetBuffer, 0, targetRec * recLen);
                        }

                        // Update the pointers
                        sourceRec += recs2Parse;
                        targetPos += targetRec * recLen;
                    }

                    // ---------------------------------------------------
                    // Close and rename the old DBF to a BAK file
                    // ---------------------------------------------------
                    await DBFClose();

                    string tFile = JAXLib.JustFullPath(fileName) + JAXLib.JustStem(fileName) + ".bak";
                    FilerLib.DeleteFile(tFile);
                    FilerLib.MoveFile(fileName, tFile);

                    string oldMemoFile = JAXLib.JustFullPath(fileName) + JAXLib.JustStem(fileName) + ".fpk";
                    string memoFile = JAXLib.JustFullPath(fileName) + JAXLib.JustStem(fileName) + ext;

                    if (hasMemo)
                    {
                        FilerLib.MoveFile(memoFile, oldMemoFile);

                        tFile = JAXLib.JustFullPath(tFile) + JAXLib.JustStem(tFile) + ext;
                        FilerLib.MoveFile(tFile, memoFile);
                    }

                    // ---------------------------------------------------
                    // Close and rename the new dbf and memo files
                    // ---------------------------------------------------
                    tFile = tmpDBF.DbfInfo.FQFN;
                    await tmpDBF.DBFClose();
                    FilerLib.MoveFile(tFile, fileName);

                    if (hasMemo)
                    {
                        tFile = JAXLib.JustFullPath(tFile) + JAXLib.JustStem(tFile) + ext;
                        FilerLib.MoveFile(tFile, memoFile);
                    }


                    // ---------------------------------------------------
                    // Open the new DBF so we can reindex everything
                    // ---------------------------------------------------
                    await DBFUse(fileName, alias, true, false, string.Empty);

                    // Call the create index routine for each index on the new dbf
                    for (int i = 0; i < indexList.Count; i++)
                        await IDXCreate(indexList[i].FileName, indexList[i].KeyClause, indexList[i].Descending, indexList[i].IsUnique, indexList[i].ForClause);

                    // ---------------------------------------------------
                    // Close the DBF, and open it in just the same manner
                    // as it was originally and set the record pointer
                    // at the top of the file
                    // ---------------------------------------------------
                    await DBFClose();
                    await DBFUse(fileName, alias, exclusive, noUpdate, connection);
                    await DBFGotoRecord("top");

                    // Open up the indexes
                    for (int i = 0; i < indexList.Count; i++)
                    {
                        await IDXOpen(indexList[i].FileName, i == controllingIDX);

                        if (i == controllingIDX)
                            await DBFGotoRecord("top");
                    }

                    // All done!  Grab a cookie and a cup of coffee!
                }
                else
                    throw new Exception("1525||Cannot pack a view or remote table");
            }
            catch (Exception ex)
            {
                // TODO - make sure the old table and memo are put back where they belong
                AppErrorHandling.SetError(8014, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }
        }


        /*-----------------------------------------------------------------------------------*
         * This moves DBF records up or down a number of rows.  If down, only 1 row is
         * inserted at the startPos record number.  A blank row is automatically appended
         * for moving down and after a move up the trailing rows are chopped off the end 
         * of the table.
         * 
         * You need to track where the record pointer; this routine just moves records
         *----------------------------------------------------------------------------------*/
        private async Task<bool> DBFMoveRecords(int moveToRec, int moveStartRec)
        {
            try
            {
                bool changed = false;

                if (moveToRec < 1 || moveStartRec < 1 || moveStartRec > DbfInfo.RecCount)
                    throw new Exception(string.Format("Invalid move parameters move to={0}, starting record={1}", moveToRec, moveStartRec));

                if (AppErrorHandling.ErrorCount() == 0 && DbfInfo.DBFStream is not null)
                {
                    // Init some variables
                    int headerLen = DbfInfo.HeaderLen;
                    int recPos = moveToRec;

                    // Are you pointing at a record in the table
                    if (recPos < DbfInfo.RecCount)
                    {
                        if (recPos < moveStartRec)
                        {
                            // We're moving records up (packing)
                            if (recPos < DbfInfo.RecCount)
                            {
                                int recs = DbfInfo.RecCount - moveStartRec;       // number of records to move
                                int recLen = DbfInfo.RecordLen;
                                int sourcePos = headerLen + (moveStartRec - 1) * recLen;
                                int targetPos = headerLen + (moveToRec - 1) * recLen;

                                while (recs > 0)
                                {
                                    changed = true;

                                    // grab 100 (or remaining if less than 100) records at a time to the
                                    // oldBuffer array and iterated through it
                                    int recs2Move = recs >= 100 ? 100 : recs;
                                    recs -= recs2Move;

                                    byte[] targetBuffer = new byte[recLen * 100];
                                    byte[] sourceBuffer = new byte[recs2Move * recLen];
                                    DbfInfo.DBFStream!.Position = sourcePos;
                                    DbfInfo.DBFStream!.ReadExactly(sourceBuffer);

                                    DbfInfo.DBFStream!.Position = targetPos;
                                    DbfInfo.DBFStream!.Write(targetBuffer, 0, targetPos);

                                    targetPos += sourceBuffer.Length;
                                }

                                if (changed)
                                {
                                    // We're done, now truncate the table length
                                    DbfInfo.DBFStream!.SetLength(targetPos);

                                    // And update the header!
                                    DbfInfo.RecCount -= moveToRec - moveStartRec;
                                    await DBFSetHeader("RC", DbfInfo.RecCount);
                                }
                            }
                            else
                                throw new Exception("4||Starting position is past end of file");
                        }
                        else
                        {
                            // We're moving records down or just appending
                            // a blank record to the end of the table
                            await DBFAppendRecord(null);

                            if (recPos < DbfInfo.RecCount - 1)
                            {
                                // It is not an append to the end of the table
                                // so get that blank record we just created to
                                // insert at the starting record location
                                int recLen = DbfInfo.RecordLen;
                                byte[] recRow = new byte[recLen];
                                DbfInfo.DBFStream.Position = headerLen + (DbfInfo.RecCount - 1) * recLen;
                                DbfInfo.DBFStream.ReadExactly(recRow, 0, recLen);

                                // Starting at the record second to the bottom, move
                                // all records down a block of 100 at a time until
                                // we get to the desired physical record
                                int moveEnd = DbfInfo.RecCount - 1;
                                while (moveEnd > recPos)
                                {
                                    int moveStart = moveEnd - 100 <= recPos ? moveEnd - 100 : recPos;
                                    int moveLen = (moveEnd - moveStart) * recLen;
                                    byte[] moveBuffer = new byte[moveLen];
                                    DbfInfo.DBFStream.Position = headerLen + (moveStart - 1) * recLen;
                                    DbfInfo.DBFStream.ReadExactly(moveBuffer, 0, moveLen);
                                    DbfInfo.DBFStream.Position = headerLen + (moveStart + 1) * recLen;
                                    DbfInfo.DBFStream.Write(moveBuffer, 0, moveLen);
                                    moveEnd = moveStart - 1;
                                }

                                // Insert the blank record here
                                DbfInfo.DBFStream.Position = headerLen + (recPos - 1) * recLen;
                                DbfInfo.DBFStream.Write(recRow, 0, recLen);
                            }
                        }
                    }
                    else
                        throw new Exception("4||Cannot start past end of table");
                }
                else
                {
                    throw new Exception("52|");
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(8015, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return AppErrorHandling.ErrorCount() == 0;
        }


        /*
         * Utility routine to update the header so we don't have to 
         * rewrite that code over and over
         */
        private async Task<bool> DBFSetHeader(string cmd, int val)
        {
            try
            {
                if (DbfInfo.DBFStream is not null)
                {
                    switch (cmd.ToUpper())
                    {
                        case "RC":
                            if (val < 0)
                                throw new Exception("2091|" + DbfInfo.TableName);

                            DbfInfo.RecCount = val;
                            byte[] recBytes = Convert.FromBase64String(App.utl.MKI(val));
                            DbfInfo.DBFStream.Position = 4;
                            DbfInfo.DBFStream.Write(recBytes, 0, 4);
                            break;

                        default:
                            // We're just going to update the date of last update
                            break;
                    }

                    // Always update the date of last update
                    byte[] lastUpdate = [(byte)(DateTime.Now.Year - DateTime.Now.Year / 100 * 100), (byte)DateTime.Now.Month, (byte)DateTime.Now.Day];
                    DbfInfo.LastUpdate = DateTime.Now.ToString("yy/MM/dd");
                    DbfInfo.DBFStream.Seek(1, SeekOrigin.Begin);
                    DbfInfo.DBFStream.Write(lastUpdate, 0, 3);
                    DbfInfo.DBFStream.Flush();
                }
                else
                    throw new Exception("52|");
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(8014, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return AppErrorHandling.ErrorCount() == 0;
        }


        // TODO
        public async Task DBFZap()
        {
            // Copy indexes headers
            // Close the table
            // Read the header
            // Set to 0 records
            // Delete the table
            // Rewrite the Header
            // Rewrite indexes
            // Reopen the table
        }


        /*===================================================================================*
         *===================================================================================*
         * Indexes - IDX 
         *-----------------------------------------------------------------------------------*
         * Indexes for JAXBase tables will have a 64 byte header
         * 
         * ----------------------------------------------
         * TODO - Indexes for JAXBase Tables start here
         * ----------------------------------------------
         * Header (64 bytes)
         *     0    0x20 - 32 bit index, 0x40 - 64 bit index
         *  1-36    36 character GUID must match table's GUID
         * 37-63    Reserved for future use
         * 
         * ----------------------------------------------
         * DBase and FoxPro Indexes start here
         * ----------------------------------------------
         * Byte offset Description
         *        00 – 03 Pointer to root node
         *        04 – 07 Pointer to free node list ( -1 if not present)
         *        08 – 11 File length
         *        12 – 13 Length of key
         *        14 - Index options (any of the following numeric values or their sums):
         *               1 – a unique index
         *               8 – index has FOR clause
         *              32 – compact index format
         *              64 –compound index header
         *        15 - Signature
         *        16 - Index Info (220 bytes)
         *       236 - Index Info (220 bytes)
         *       502 – 503  Ascending or descending:
         *               0 = ascending
         *               1 = descending
         *               
         * Searching for a Value
         *
         *      Start at the first Index Node.
         *      
         *      Each record of an Index Node points to the file position of leaf nodes and 
         *      the key value of that record is the highest value of that node.  Searching
         *      through the index nodes (moving right), you grab the record where that key
         *      is greater than or equal to the key you are comparing and use that value
         *      to go to the correct leaf node.
         *      
         *      You will then go to that leaf node and look for your key.
         *      
         *      If you run out of Index Nodes before finding a key greater than or equal
         *      to your value, you know it's not in the index.
         *      
         *      Notes:
         *          If EXACT is set on, you'll need an exact match, othewise you can 
         *          just compare the trimmed string, or precision of the seaarched number.
         *              Examples for EXACT = off: 
         *                  "Abc" is equal to "Abcd"
         *                  1.1 is equal to 1.12
         *      
         *          If NEAR is set on, you'll position at the last record of the table, else
         *          you will position after the last record, EOF is turned on, and a blank
         *          record is put into the DbfInfo.CurrentRecord.
         *          
         *          NEAR = ON is will return similar results to EXACT = OFF
         *          
         * TODO - JAXBase GUID tag support
         *-----------------------------------------------------------------------------------*/
        public async Task<bool> IDXOpen(string FullFileName)
        {
            return await IDXOpen(FullFileName, true);
        }

        public async Task<bool> IDXOpen(string FullFileName, bool getRec)
        {
            bool llSuccess = true;
            DbfInfo.ControllingIDX = -1;

            bool notOpen = true;

            string stem = JAXLib.JustStem(FullFileName);
            for (int i = 0; i < DbfInfo.IDX.Count; i++)
            {
                if (DbfInfo.IDX[0].Name.Equals(stem, StringComparison.OrdinalIgnoreCase))
                {
                    DbfInfo.ControllingIDX = i;
                    notOpen = false;

                    if (FullFileName.Equals(DbfInfo.IDX[i].FileName) == false)
                    {
                        notOpen = false;
                        await IDXClose(i);
                    }
                    else
                    {
                        // TODO - Make sure we're at the correct record
                    }
                    break;
                }
            }

            if (notOpen)
            {
                try
                {
                    if (File.Exists(FullFileName))
                    {
                        // Open the file and read header
                        byte[] idxBuffer = new byte[512];
                        byte[] buffer;
                        IDXInfo idxInfo = new();

                        try
                        {
                            FullFileName = JAXLib.JustExt(FullFileName).Length == 0 ? (FullFileName.TrimEnd('.') + ".idx") : FullFileName;
                            idxInfo.IDXStream = new FileStream(FullFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                            idxInfo.FileLen = (int)DbfInfo.DBFStream!.Length;
                            idxInfo.IDXStream!.ReadExactly(idxBuffer, 0, 512);
                            idxInfo.FileName = FullFileName;
                            idxInfo.Name = stem;
                        }
                        catch (Exception ex)
                        {
                            // If IO error then we could not open the file
                            AppErrorHandling.SetError(3, "Error opening index " + FullFileName + " - " + ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        }


                        // Break the header out into an IDXInfo class and
                        // add it to the list in DbfInfo
                        if (AppErrorHandling.ErrorCount() == 0)
                        {
                            // Root Node (integer)
                            buffer = new byte[4];
                            Array.Copy(idxBuffer, 0, buffer, 0, 4);
                            idxInfo.RootNode = App.utl.CVI(Convert.ToBase64String(buffer));

                            // Free Node (integer)
                            Array.Copy(idxBuffer, 4, buffer, 0, 4);
                            idxInfo.FreeNode = App.utl.CVI(Convert.ToBase64String(buffer));

                            // File Length (integer)
                            Array.Copy(idxBuffer, 8, buffer, 0, 4);     // PUT IN CHECK AND FIX HERE!
                            idxInfo.FileLen = App.utl.CVI(Convert.ToBase64String(buffer));

                            // Key Length (short unsigned integer)
                            buffer = new byte[2];
                            Array.Copy(idxBuffer, 12, buffer, 0, 2);
                            idxInfo.KeyLen = App.utl.CVU(Convert.ToBase64String(buffer));

                            idxInfo.MaxKeys = (512 - 12) / (idxInfo.KeyLen + 4);

                            // Ascending/Descending flag (short unsigned integer)
                            Array.Copy(idxBuffer, 502, buffer, 0, 2);
                            idxInfo.Descending = App.utl.CVU(Convert.ToBase64String(buffer)) > 0;

                            // Options (byte)
                            idxInfo.Options = idxBuffer[14];
                            idxInfo.IsUnique = (idxInfo.Options & 0x01) > 0;
                            idxInfo.HasFor = (idxInfo.Options & 0x08) > 0;
                            idxInfo.IsCompactIDX = (idxInfo.Options & 0x20) > 0;
                            idxInfo.IsCompoundIDX = (idxInfo.Options & 0x40) > 0;

                            // Signature (byte)
                            idxInfo.Signature = idxBuffer[15];

                            //  Index key
                            buffer = new byte[220];
                            Array.Copy(idxBuffer, 16, buffer, 0, 220);
                            idxInfo.KeyClause = Encoding.ASCII.GetString(buffer).Replace("\0", "");

                            // For clause
                            Array.Copy(idxBuffer, 236, buffer, 0, 220);
                            idxInfo.ForClause = Encoding.ASCII.GetString(buffer).Replace("\0", "");

                            idxInfo.IDXListPos = DbfInfo.IDX.Count;
                            DbfInfo.IDX.Add(idxInfo);
                            DbfInfo.ControllingIDX = DbfInfo.IDX.Count - 1;

                            if (PRINTDEBUG)
                            {
                                AppIO.DebugLog("");
                                AppIO.DebugLog(string.Format("    Index {0}", FullFileName));
                                AppIO.DebugLog(string.Format("    File Length = {0}", DbfInfo.IDX[DbfInfo.ControllingIDX].FileLen));
                                AppIO.DebugLog(string.Format("    Root Node   = {0}", DbfInfo.IDX[DbfInfo.ControllingIDX].RootNode));
                                AppIO.DebugLog(string.Format("    Key Length  = {0}", DbfInfo.IDX[DbfInfo.ControllingIDX].KeyLen));
                                AppIO.DebugLog(string.Format("    Unique      = {0}", DbfInfo.IDX[DbfInfo.ControllingIDX].IsUnique));
                                AppIO.DebugLog(string.Format("    Descending  = {0}", DbfInfo.IDX[DbfInfo.ControllingIDX].Descending));
                                AppIO.DebugLog(string.Format("    Compact     = {0}", DbfInfo.IDX[DbfInfo.ControllingIDX].IsCompactIDX));
                                AppIO.DebugLog(string.Format("    Compound    = {0}", DbfInfo.IDX[DbfInfo.ControllingIDX].IsCompoundIDX));
                                AppIO.DebugLog(string.Format("    Signature   = {0}", DbfInfo.IDX[DbfInfo.ControllingIDX].Signature));
                                AppIO.DebugLog(string.Format("    Key Clause  = {0}", DbfInfo.IDX[DbfInfo.ControllingIDX].KeyClause));
                                AppIO.DebugLog(string.Format("    For Cluase  = {0}", DbfInfo.IDX[DbfInfo.ControllingIDX].ForClause));
                                AppIO.DebugLog("");

                                //for (int i = 0; i < 16; i++) AppIO.DebugLog(string.Format("Byte {0} = {1}", i, idxBuffer[i]));
                                AppIO.DebugLog("");
                                AppIO.DebugLog("");
                            }

                            // Position the index on the first record of the index
                            if (getRec)
                                await DBFGotoRecord("top");
                        }
                    }
                    else
                        throw new Exception("1|" + FullFileName);
                }
                catch (Exception ex)
                {
                    AppErrorHandling.SetError(8016, $"8016|{FullFileName}|" + ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                }
            }

            return llSuccess;
        }


        /* 
         * TODO - close a named index by type
         * 
         * idxName  - Name of index to close
         */
        public async Task CDXClose(string idxName, string type)
        {

        }

        public async Task IDXClose(int idx)
        {
            if (idx >= 0 && idx < DbfInfo.IDX.Count)
            {
                // Are we closing the controlling index?
                if (DbfInfo.ControllingIDX == idx)
                    DbfInfo.ControllingIDX = -1;

                // Close this index
                DbfInfo.IDX[idx].IDXStream?.Close();
                DbfInfo.IDX.RemoveAt(idx);

                // Correct the controlling index?
                if (DbfInfo.ControllingIDX > idx)
                    DbfInfo.ControllingIDX--;
            }

        }

        /*-----------------------------------------------------------------------------------*
         * Get a list of all indexes that match based on name and/or type
         * 
         * If no name is supplied, all indexes are eligle for return
         * 
         * Filtering by type:
         *  Type="I" - normal IDXs in order they were opened
         *  Type="C" - structural CDX tags in order they are found in CDX
         *  Type="X" - Independent compound indexes in order they are found in CDX
         *  
         *-----------------------------------------------------------------------------------*/
        public async Task<List<IDXInfo>> IDXGetInfoList(string idxName, string type)
        {
            List<IDXInfo> idxInfo = [];

            for (int i = 0; i < DbfInfo.IDX.Count; i++)
            {
                if (type.Length == 0 ||
                    type.Equals("I") && DbfInfo.IDX[i].IsCDX == false
                    || type.Equals("C") && DbfInfo.IDX[i].IsCDX && DbfInfo.IDX[i].Name.Equals(DbfInfo.TableName, StringComparison.OrdinalIgnoreCase)
                    || type.Equals("X") && DbfInfo.IDX[i].IsCDX && DbfInfo.IDX[i].Name.Equals(DbfInfo.TableName, StringComparison.OrdinalIgnoreCase) == false
                    )
                {
                    // type match so check for name match
                    if (idxName.Length == 0 || idxName.Equals(DbfInfo.IDX[i].Name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (DbfInfo.IDX[i].IDXListPos != i) DbfInfo.IDX[i].IDXListPos = i;
                        idxInfo.Add(DbfInfo.IDX[i]);
                    }
                }
            }

            return idxInfo;
        }


        /// <summary>
        /// Create a header for an index based on the IDXInfo class provided and use it to create the new 
        /// index (IDX) file.
        /// </summary>
        /// <param name="idxInfo"></param>
        public async Task IDXCreateHeader(IDXInfo idxInfo)
        {

            try
            {
                int rootPos = idxInfo.RootNode;
                if (rootPos < 512) throw new Exception("SysErr - Invalid root node position");

                byte[] buffer = new byte[512];
                byte[] node = new byte[512];

                // -------------------------------------------------------------
                // CREATE THE IDX HEADER
                // -------------------------------------------------------------
                int RootNode = rootPos;
                byte[] bv = Convert.FromBase64String(App.utl.MKI(RootNode));
                Array.Copy(bv, 0, buffer, 0, bv.Length);

                int MaxKeys = (512 - 12) / (idxInfo.KeyLen + 4);

                int FreeNode = -1;
                bv = Convert.FromBase64String(App.utl.MKI(FreeNode));
                Array.Copy(bv, 0, buffer, 4, bv.Length);

                int FileLen = 1024;
                bv = Convert.FromBase64String(App.utl.MKI(FileLen));
                Array.Copy(bv, 0, buffer, 8, bv.Length);

                bv = Convert.FromBase64String(App.utl.MKU((ushort)idxInfo.KeyLen));
                Array.Copy(bv, 0, buffer, 12, bv.Length);

                buffer[14] = (byte)((idxInfo.IsUnique ? 1 : 0) + (idxInfo.ForClause.Length > 0 ? 8 : 0)
                    + (idxInfo.IsCompactIDX ? 16 : 0) + (idxInfo.IsCompoundIDX ? 64 : 0));

                buffer[15] = (byte)idxInfo.Signature;

                bv = Convert.FromBase64String(App.utl.MKU((ushort)(idxInfo.Descending ? 1 : 0)));
                Array.Copy(bv, 0, buffer, 502, bv.Length);

                bv = Encoding.ASCII.GetBytes(idxInfo.KeyClause);
                Array.Copy(bv, 0, buffer, 16, bv.Length);

                bv = Encoding.ASCII.GetBytes(idxInfo.ForClause);
                Array.Copy(bv, 0, buffer, 236, bv.Length);

                // -------------------------------------------------------------
                // CREATE THE IDX ROOT NODE
                // -------------------------------------------------------------
                // Attributes = Root Node & Leaf Node
                node[0] = 3;

                // Keys = 0 (bytes 2 & 3)
                AppIO.DebugLog(string.Format(""));
                AppIO.DebugLog(string.Format("Creating Header for {0}", idxInfo.FileName));
                AppIO.DebugLog(string.Format("Root Pos   = {0}", idxInfo.RootNode));
                AppIO.DebugLog(string.Format("Max Keys   = {0}", (512 - 12) / (idxInfo.KeyLen + 4)));
                AppIO.DebugLog(string.Format("Key Len    = {0}", idxInfo.KeyLen));
                AppIO.DebugLog(string.Format("Key Clause = {0}", idxInfo.KeyClause));
                AppIO.DebugLog(string.Format("For Clause = {0}", idxInfo.ForClause));

                // Left & Right pointer
                bv = Convert.FromBase64String(App.utl.MKI(-1));
                Array.Copy(bv, 0, node, 4, bv.Length);
                Array.Copy(bv, 0, node, 8, bv.Length);

                // -------------------------------------------------------------
                // WRITE THE HEADER AND ROOT NODE
                // -------------------------------------------------------------
                if (File.Exists(idxInfo.FileName))
                    File.Delete(idxInfo.FileName);

                using (var fs = new FileStream(idxInfo.FileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    // Write the header bytes to the file and close it up
                    fs.Write(buffer, 0, buffer.Length);
                    fs.Write(node, 0, node.Length);
                    fs.Flush();
                }
            }
            catch (Exception ex)
            {
                // Execution error
                AppErrorHandling.SetError(8014, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }
        }


        /*-----------------------------------------------------------------------------------*
         * Create an IDX for the current open DBF - Overwrite if it exists
         * 
         * Create a list of all keys and record numbers, put them into the leafRecords list
         * Create a list of the number of indexes we'll need, put them into the indexRows
         * list of lists.
         * Create a leadIndex list taking ever x one based on MaxKeys
         * Fill in the last row of indexRows using the leafIndex
         * Fill in the rest of the indexRows using the row below it
         *-----------------------------------------------------------------------------------*/
        /// <summary>
        /// Create an IDX for the current open DBF - Overwrite if it exists
        /// </summary>
        /// <param name="FullFileName"></param>
        /// <param name="keyExpr"></param>
        /// <param name="isdesc"></param>
        /// <param name="isunique"></param>
        /// <param name="forExpr"></param>
        /// <returns></returns>
        public async Task<bool> IDXCreate(string FullFileName, string keyExpr, bool isdesc, bool isunique, string forExpr)
        {
            bool llSuccess = true;
            JAXMath jaxMath = new();
            GenericClass gc;

            // Get the stem name of the index file name
            string idxName = JAXLib.JustStem(FullFileName);
            int keyLen = 0;
            int idx = -1;
            List<IDXRecord> LeafRecords = [];
            IDXInfo idxInfo = new();


            try
            {
                // If there are open indexes, look for this one
                // and close it so we can rebuild it
                if (DbfInfo.IDX.Count > 0)
                {
                    // Look for an open index with the same stem
                    int thisIDX = DbfInfo.IDX.FindIndex(x => x.IsCDX == false && x.Name.Equals(idxName, StringComparison.OrdinalIgnoreCase));
                    if (thisIDX >= 0)
                    {
                        // Close this index
                        DbfInfo.IDX[thisIDX].IDXStream?.Close();
                        DbfInfo.IDX.RemoveAt(thisIDX);
                    }

                    // Look for an open index with the same FQFN
                    thisIDX = DbfInfo.IDX.FindIndex(x => x.IsCDX == false && x.FileName.Equals(FullFileName.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (thisIDX >= 0)
                    {
                        // Close this index
                        DbfInfo.IDX[thisIDX].IDXStream?.Close();
                        DbfInfo.IDX.RemoveAt(thisIDX);
                    }
                }

                // Get the key length based on the results of the expression
                DbfInfo.ControllingIDX = -1;
                await DBFGotoRecord(1);

                // Make sure that field names return correct empty values
                // and char data is all max length so we can get an accurate
                // expression length for the index header
                DbfInfo.CreatingIDXExpression = true;
                gc = await jaxMath.SolveMath(keyExpr);
                DbfInfo.CreatingIDXExpression = false;

                // Get the key length based on the data type
                string keyType = gc.Value.Element.Type;
                keyLen = keyType switch
                {
                    "C" => gc.Value.Element.ValueAsString.Length,
                    "L" => 1,
                    "I" => 4,
                    "D" => 8,
                    "T" => 8,
                    "N" => 8,
                    "B" => 8,
                    _ => throw new Exception("Invalid key")
                };

                // Set up the IDXInfo class
                idxInfo = new()
                {
                    FileName = FullFileName,
                    Name = idxName,
                    KeyClause = keyExpr.Trim(),
                    Descending = isdesc,
                    IsCDX = false,
                    IsCompactIDX = false,
                    IsCompoundIDX = false,
                    IsUnique = isunique,
                    HasFor = forExpr.Length > 0,
                    KeyLen = keyLen,
                    ForClause = forExpr.Trim()
                };

                if (idxInfo.IsCDX)
                {
                    // TODO - It's a CDX so special handling of root assignment is requried
                    if (keyLen > 240) throw new Exception("112|");
                }
                else
                {
                    if (keyLen > 100) throw new Exception("112|");
                    idxInfo.RootNode = 512; // IDX always starts at 512
                }
                // Check the for clause to make sure it's valid
                if (idxInfo.HasFor)
                {
                    gc = await jaxMath.SolveMath(idxInfo.ForClause);
                    if (gc.Value.Element.Type.Equals("L", StringComparison.OrdinalIgnoreCase) == false)
                        throw new Exception("Invalid for clause");
                }

                // Write the file header
                await IDXCreateHeader(idxInfo);
            }
            catch (Exception ex)
            {
                llSuccess = false;
                AppErrorHandling.SetError(8017, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            LeafRecords = [];
            int testRec = 0;
            int keyCompare = 0;
            int InsertAt = 0;
            int StartAt = 0;
            int EndAt = 0;
            int moveRec = 0;
            IDXRecord r = new();
            byte[] keyBytes = [];

            try
            {
                if (llSuccess)
                {
                    // Open it up like a normal index file and start the process
                    await IDXOpen(FullFileName, false);
                    idx = DbfInfo.ControllingIDX;
                    DbfInfo.ControllingIDX = -1;
                    DbfInfo.IDX[idx].IOLock = true;

                    string keyClause = DbfInfo.IDX[idx].KeyClause;
                    string forClause = DbfInfo.IDX[idx].ForClause;

                    if (PRINTDEBUG)
                    {
                        Console.WriteLine("");
                        Console.WriteLine(string.Format("Creating index {0}", idxInfo.Name));
                    }

                    // -------------------------------------------------------
                    // Create the sorted leaf records list
                    // -------------------------------------------------------
                    for (int i = 1; i <= DbfInfo.RecCount; i++)
                    {
                        await DBFGotoThisRecord(i);

                        bool OK2Add = true;
                        int recNo = DbfInfo.RecNo;

                        if (DbfInfo.IDX[idx].HasFor)
                        {
                            // Look at the FOR clause
                            gc = await jaxMath.SolveMath(forClause);
                            OK2Add = gc.Value.Element.ValueAsBool;
                        }

                        if (OK2Add)
                        {
                            gc = await jaxMath.SolveMath(keyClause);
                            keyBytes = await IDXGetKey(gc.Value, keyLen);

                            //if (gc.Value.Element.ValueAsString.Contains("ZOTERA LLC", StringComparison.OrdinalIgnoreCase))
                            //    OK2Add = true;

                            r = new()
                            {
                                Key = keyBytes,
                                RecPos = i
                            };

                            InsertAt = LeafRecords.Count > 0 ? -9999 : -1;  // Don't add flag is -9999
                            StartAt = 0;
                            EndAt = LeafRecords.Count;
                            moveRec = EndAt / 2;


                            while (StartAt < EndAt)
                            {
                                if (moveRec < 4)
                                {
                                    // Less than 4 records away from correct
                                    // position, so go look at each of them
                                    InsertAt = -1;

                                    for (int j = StartAt; j < EndAt; j++)
                                    {
                                        keyCompare = StructuralComparisons.StructuralComparer.Compare(keyBytes, LeafRecords[j].Key);

                                        if (keyCompare < 0)
                                        {
                                            // Less than record key, insert here
                                            InsertAt = j;
                                            break;
                                        }
                                        else if (keyCompare == 0 && DbfInfo.IDX[idx].IsUnique)
                                        {
                                            InsertAt = -9999;
                                            break;   // Don't add unique record already in list
                                        }
                                    }

                                    break;
                                }
                                else
                                {
                                    // Make sure the move doesn't go more than half way
                                    moveRec = (EndAt - StartAt) / 2 < moveRec ? (EndAt - StartAt) / 2 : moveRec;
                                    testRec = StartAt + moveRec;
                                    keyCompare = StructuralComparisons.StructuralComparer.Compare(keyBytes, LeafRecords[testRec].Key);

                                    if (keyCompare < 0)
                                    {
                                        // We're past the correct location so bring
                                        // the endAt up to this location +1 to make
                                        // sure we don't miss something
                                        EndAt = StartAt + moveRec + 1;
                                        moveRec = (EndAt - StartAt) / 2;
                                    }
                                    else if (keyCompare == 0)
                                    {
                                        // We found the key, but we need to get 
                                        // past it. Move StartAt to this record.
                                        StartAt = testRec;
                                    }
                                    else
                                    {
                                        // not far enough, move start downward
                                        StartAt += moveRec;
                                        moveRec = (EndAt - StartAt) / 2;
                                    }
                                }
                            }

                            if (InsertAt < 0)
                            {
                                if (InsertAt == -1) LeafRecords.Add(r);
                            }
                            else
                                LeafRecords.Insert(InsertAt, r);

                            if (PRINTDEBUG && i % 100 == 0)
                                Console.WriteLine(string.Format("{0} records indexed", i));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                llSuccess = false;
                AppErrorHandling.SetError(8017, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            try
            {
                if (llSuccess)
                {
                    // -------------------------------------------------------
                    // We have the leaf data, now put together blank
                    // root and index nodes into a list of lists with
                    // the root node in the lowest index
                    // -------------------------------------------------------
                    int maxKeys = DbfInfo.IDX[idx].MaxKeys;

                    if (LeafRecords.Count > maxKeys)
                    {
                        // We will be creating a root and at least 1 index node
                        List<List<IDXRecord>> indexRows = [];
                        List<IDXRecord> indexRecords = [];
                        int nodePosition = 0;

                        try
                        {

                            // bottom row of Index nodes handles maxKeys*maxKeys leaf rows
                            indexRecords = await IDXCreateIndexNodes(LeafRecords, maxKeys * maxKeys);
                            indexRows.Add(indexRecords);

                            // Work your way up to the root node with each of the remaining
                            // nodes handling maxKey nodes below it
                            while (indexRecords.Count > 1)
                            {
                                indexRecords = await IDXCreateIndexNodes(indexRecords, maxKeys);
                                indexRows.Insert(0, indexRecords);
                            }

                            // Write out blank root and index nodes.  Record the
                            // positions to the records for later use
                            nodePosition = DbfInfo.IDX[idx].RootNode;
                            for (int j = 0; j < indexRows.Count; j++)
                            {
                                for (int k = 0; k < indexRows[j].Count; k++)
                                {
                                    indexRows[j][k].Position = nodePosition;
                                    await IDXWrite(idx, nodePosition, new byte[512]);
                                    nodePosition += 512;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            llSuccess = false;
                            AppErrorHandling.SetError(8017, "Error creating blank index nodes - " + ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        }


                        try
                        {
                            // -------------------------------------------------------
                            // Write out the leaf nodes while filling in the 
                            // position of the destination node in each record
                            // -------------------------------------------------------
                            int keys = 0;
                            byte[] buffer = new byte[512];
                            List<IDXRecord> indexRow = indexRows[^1];
                            List<IDXRecord> leafIndex = [];
                            int leafRec = maxKeys - 1;
                            buffer = new byte[512];

                            // new
                            int leafNodeCount = indexRow.Count;
                            leafIndex.Add(new IDXRecord());

                            for (int i = 0; i < indexRow.Count * maxKeys; i++)
                            {
                                for (int j = 0; j < maxKeys; j++)
                                {
                                    // Which leaf record are we working on?
                                    leafRec = i * maxKeys + j;

                                    if (leafRec < LeafRecords.Count)
                                    {
                                        int keyPos = 12 + keys++ * (keyLen + 4);
                                        byte[] recPos = App.utl.RevInt2Bin(LeafRecords[leafRec].RecPos);
                                        Array.Copy(LeafRecords[leafRec].Key, 0, buffer, keyPos, keyLen);
                                        Array.Copy(recPos, 0, buffer, keyPos + keyLen, 4);
                                        //LeafRecords[leafRec].Position = nodePosition;

                                        // new
                                        leafIndex[^1].Key = LeafRecords[leafRec].Key;
                                        leafIndex[^1].Position = nodePosition;
                                    }
                                    else
                                    {
                                        // We're done
                                        leafRec = LeafRecords.Count - 1;
                                        break;
                                    }
                                }

                                // Set the header and write the leaf node
                                IDXNode node = new()
                                {
                                    Buffer = buffer,
                                    Attributes = 2,
                                    Keys = keys,
                                    LeftPtr = i > 0 ? nodePosition - 512 : -1,
                                    RightPtr = leafRec < LeafRecords.Count - 1 ? nodePosition + 512 : -1,
                                };

                                await IDXWrite(idx, nodePosition, node.Buffer);
                                nodePosition += 512;

                                leafIndex.Add(new IDXRecord());             // new
                                keys = 0;
                                buffer = new byte[512];

                                if (leafRec >= LeafRecords.Count - 1) break;
                            }

                            if (leafIndex[^1].RecPos == 0)                    // new
                                leafIndex.RemoveAt(leafIndex.Count - 1);

                            // -------------------------------------------------------
                            // Fill in the last index row using the leaf records
                            // where maxKeys leaf records are represented in each
                            // row of the index node.  Thus each index node in
                            // this row is responsible for maxKeys * maxKeys
                            // records while the rest of the indexes just point
                            // to maxkey indexes blow them.
                            // -------------------------------------------------------
                            //IDXFillAndWrite(indexRows[^1], LeafRecords, maxKeys, maxKeys, keyLen, idx);   // old
                            await IDXFillAndWrite(indexRows[^1], leafIndex, 1, maxKeys, keyLen, idx);             // new

                            // Fill in the remaining index records using the row below it
                            // where each node below is one record in the index node below
                            for (int i = indexRows.Count - 2; i >= 0; i--)
                                await IDXFillAndWrite(indexRows[i], indexRows[i + 1], 1, maxKeys, keyLen, idx);
                        }
                        catch (Exception ex)
                        {
                            llSuccess = false;
                            AppErrorHandling.SetError(8017, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        }
                    }
                    else
                    {
                        // -------------------------------------------------------
                        // Just create a root/leaf node and add the keys to it
                        // -------------------------------------------------------
                        IDXNode node = new();
                        byte[] buffer = new byte[512];
                        int keys = LeafRecords.Count;

                        for (int k = 0; k < keys; k++)
                        {
                            int keyPos = 12 + k * (keyLen + 4);
                            byte[] recPos = App.utl.RevInt2Bin(LeafRecords[k].RecPos);
                            Array.Copy(LeafRecords[k].Key, 0, buffer, keyPos, keyLen);
                            Array.Copy(recPos, 0, buffer, keyPos + keyLen, 4);
                        }

                        node.Buffer = buffer;
                        node.Attributes = 3;
                        node.LeftPtr = -1;
                        node.RightPtr = -1;
                        node.Keys = keys;
                        node.Position = 512;

                        await IDXWrite(idx, node.Position, node.Buffer);
                    }
                }
            }
            catch (Exception ex)
            {
                llSuccess = false;
                AppErrorHandling.SetError(8017, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            // Remember to unlock it
            if (idx >= 0)
                DbfInfo.IDX[idx].IOLock = false;

            return llSuccess;
        }


        /*-----------------------------------------------------------------------------------*
         *-----------------------------------------------------------------------------------*/
        private async Task IDXFillAndWrite(List<IDXRecord> indexRecords, List<IDXRecord> sourceRecords, int keyCount, int maxKeys, int keyLen, int idx)
        {
            AppIO.DebugLog(string.Format("Entering {0}", System.Reflection.MethodBase.GetCurrentMethod()!.Name));

            int sourceRec = keyCount - 1;
            int keys = 0;

            try
            {
                for (int i = 0; i < indexRecords.Count; i++)
                {
                    IDXNode node = new();
                    byte[] buffer = new byte[512];
                    for (int k = 0; k < maxKeys; k++)
                    {
                        if (sourceRec > sourceRecords.Count - 1)
                        {
                            if (keyCount > 1)
                                sourceRec = sourceRecords.Count - 1;
                            else
                                break;
                        }

                        keys = k + 1;
                        int keyPos = 12 + k * (keyLen + 4);
                        byte[] recPos = App.utl.RevInt2Bin(sourceRecords[sourceRec].Position);
                        Array.Copy(sourceRecords[sourceRec].Key, 0, buffer, keyPos, keyLen);
                        Array.Copy(recPos, 0, buffer, keyPos + keyLen, 4);

                        if (keyCount > 1 && sourceRec >= sourceRecords.Count - 1) break;
                        sourceRec += keyCount;
                    }

                    Console.WriteLine(string.Format("Writing node {0} for leaf {1}", i, sourceRec));
                    node.Buffer = buffer;
                    node.Attributes = indexRecords.Count > 1 ? 0 : 1;
                    node.LeftPtr = i > 0 ? indexRecords[i - 1].Position : -1;
                    node.RightPtr = i < indexRecords.Count - 1 ? indexRecords[i + 1].Position : -1;
                    node.Keys = keys;

                    await IDXWrite(idx, indexRecords[i].Position, node.Buffer);
                    keys = 0;
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(8017, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

        }



        /*-----------------------------------------------------------------------------------*
         * Create the index node tree
         * Each index node on the lowest branch represents maxKeys*maxKeys leaf nodes
         * Each index node above that represents a power of maxKeys higher than the last
         * 
         * Example:  10 records per node
         *         1 Root index    = 10 level 1 index records
         *        10 Level 1 index = 100 level 2 index records
         *       100 Level 2 index = 1,000 level 3 index records
         *      1000 Level 3 index = 100,000 records
         *-----------------------------------------------------------------------------------*/
        private async Task<List<IDXRecord>> IDXCreateIndexNodes(List<IDXRecord> records, int maxKeys)
        {
            AppIO.DebugLog(string.Format("Entering {0}", System.Reflection.MethodBase.GetCurrentMethod()!.Name));

            List<IDXRecord> nodeRecords = [];
            int nodeCount = records.Count / (maxKeys) + (records.Count % maxKeys == 0 ? 0 : 1);
            int j = maxKeys - 1;
            for (int i = 0; i < nodeCount; i++)
            {
                IDXRecord record = new();
                if (j < records.Count)
                {
                    record.Key = records[j].Key;
                    nodeRecords.Add(record);
                }

                j += maxKeys;
            }

            // Pick up the last record if we need it
            if (nodeRecords.Count < nodeCount)
            {
                IDXRecord record = new()
                {
                    Key = records[^1].Key
                };

                nodeRecords.Add(record);
            }

            return nodeRecords;
        }



        /// <summary>
        /// Convert a token value to a byte array of specified length
        /// </summary>
        /// <param name="slAnswer"></param>
        /// <param name="keyLen"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<byte[]> IDXGetKey(JAXObjects.Token slAnswer, int keyLen)
        {
            AppIO.DebugLog(string.Format("Entering {0}", System.Reflection.MethodBase.GetCurrentMethod()!.Name));

            //string thisKey;
            byte[] keyBytes = new byte[keyLen];
            byte[] myBytes = [];
            string keyType = slAnswer.Element.Type;

            switch (keyType)
            {
                case "V":
                case "C":   // Character (fix length)
                    keyBytes = [.. Enumerable.Repeat((byte)0x20, keyLen)];  // Space fill the index key
                    myBytes = Encoding.UTF8.GetBytes(slAnswer.Element.ValueAsString);
                    break;

                case "L":   // Logical (1 byte)
                    myBytes = new byte[1];
                    myBytes[0] = (byte)(slAnswer.Element.ValueAsBool ? 0x54 : 0x46);
                    break;

                case "D":   // Date saved as an integer
                    DateOnly doval = slAnswer.AsDate();
                    int dv = (doval.Year * 10000) + (doval.Month * 100) + doval.Day;
                    myBytes = Convert.FromBase64String(App.utl.MKI(-dv));
                    myBytes = [.. myBytes.Reverse()];

                    // special case for blank date
                    if (doval == DateOnly.MinValue)
                        myBytes = [0x80, 0, 0, 0];
                    break;

                case "I":
                    myBytes = Convert.FromBase64String(App.utl.MKI(-slAnswer.Element.ValueAsInt));
                    myBytes = (byte[])myBytes.Reverse();                         // make it most significant byte first

                    // Make sure zer drops in the correct order
                    if (slAnswer.Element.ValueAsInt == 0D)
                        myBytes = [0x80, 0, 0, 0];
                    break;

                case "T":                                       // store as array of 2 integers
                    DateTime dtval = slAnswer.AsDateTime();
                    int dval = (dtval.Year * 10000) + (dtval.Month * 100) + dtval.Day;
                    int tval = (dtval.Hour * 10000) + (dtval.Minute * 100) + dtval.Second;
                    byte[] dbytes = Convert.FromBase64String(App.utl.MKI(-dval));
                    byte[] tbytes = Convert.FromBase64String(App.utl.MKI(-tval));
                    dbytes = [.. dbytes.Reverse()];
                    tbytes = [.. tbytes.Reverse()];

                    // Special case for blank date/time
                    if (dtval == DateTime.MinValue)
                    {
                        dbytes = [0x80, 0, 0, 0];
                        tbytes = [0x80, 0, 0, 0];
                    }

                    myBytes = new byte[8];
                    Array.Copy(dbytes, 0, myBytes, 0, 4);
                    Array.Copy(tbytes, 0, myBytes, 4, 4);
                    break;

                case "B":
                case "N":   // TODO - make sure numbers are stored like doubles
                    double dd = -slAnswer.Element.ValueAsDouble;
                    string ds = App.utl.MKD(dd);
                    myBytes = Convert.FromBase64String(ds);
                    myBytes = [.. myBytes.Reverse()];                         // make it most significant byte first

                    // Make sure zero drops in the correct order
                    if (slAnswer.Element.ValueAsDouble == 0D)
                        myBytes = [0x80, 0, 0, 0, 0, 0, 0, 0];

                    break;

                default:
                    throw new Exception(string.Format("I can't support data type {0}", keyType));
            }


            // Make sure the length is valid
            if (myBytes.Length <= keyLen)
                Array.Copy(myBytes, 0, keyBytes, 0, myBytes.Length);
            else
                throw new Exception("Key length error");

            return keyBytes;
        }


        /*-----------------------------------------------------------------------------------*
         *  Update the specified index using the current record from the dbf
         *-----------------------------------------------------------------------------------*/
        private async Task<bool> IDXUpdateIndex(int idx)
        {
            AppIO.DebugLog(string.Format("Entering {0}", System.Reflection.MethodBase.GetCurrentMethod()!.Name));

            bool success = true;

            bool OK2Add = true;
            JAXMath jaxMath = new();
            int recNo = DbfInfo.RecNo;
            int keyLen = DbfInfo.IDX[idx].KeyLen;
            byte[] keyBytes;
            GenericClass gc;

            try
            {
                if (DbfInfo.IDX[idx].HasFor)
                {
                    // Look at the FOR clause
                    gc = await jaxMath.SolveMath(DbfInfo.IDX[idx].ForClause);
                    OK2Add = gc.Value.Element.ValueAsBool;
                }

                // If we can add this record, then do it
                if (OK2Add)
                {
                    gc = await jaxMath.SolveMath(DbfInfo.IDX[idx].KeyClause);
                    keyBytes = await IDXGetKey(gc.Value, keyLen);
                    IDXCommand idxCmd = await IDXSearchByKey(idx, keyBytes, recNo, true, false);

                    /*
                     * Parse the command and perform the add/update
                     * 
                     *   1 = Found key at this node's position and record
                     *   2 = Append to this leaf
                     *   3 = Insert to this leaf at RecNo
                     *   4 = Create a new leaf to the left and split at
                     *   5 = Create a new leaf to the right and split at
                     *   9 = Found near match
                     *  20 = Create a new index left
                     *  21 = Create a new index right
                     *  30 = Create a new root left
                     *  31 = Create a new root right
                     *  32 = Add a new index node below
                     *  33 = Add a new leaf node below
                     *  -1 = Record is not and should be part of this index (index is out of date)
                     * -99 = Error encountered
                     */

                    switch (idxCmd.Command)
                    {
                        case 0: break;  // Nothing to do!
                        case 1: break;  // Uniqueness situation

                        case 2: // Append to leaf
                            if (await IDXAppendKeyToNode(idx, idxCmd, keyBytes, recNo, idxCmd.NodeMap.Count - 1) == false)
                                throw new Exception(string.Format("Append Error"));
                            break;

                        case 3:
                            if (await IDXInsertKeyInNode(idx, idxCmd) == false)
                                throw new Exception(string.Format("Insert Error"));
                            break;

                        case 4:
                            throw new Exception(string.Format("Unsupported IDX command {0}", idxCmd.Command));

                        case 5:
                            if (await IDXInsertLeafNodeRight(idxCmd) == false)
                                throw new Exception(string.Format("Split Error"));
                            break;

                        case 9:
                        case 20:
                        case 21:
                        case 30:
                        case 31:
                        case 32:
                        case 33:
                        default:
                            throw new Exception(string.Format("Unsupported IDX command {0}", idxCmd.Command));
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                AppErrorHandling.SetError(8017, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return success;
        }


        /*-----------------------------------------------------------------------------------*
         * We need to add a new leaf node to the right so check the index node
         * above and see if there is room to add to it.  Also need special handling
         * if that node is the root node.  
         * 
         * Then we may need to add an index node if it's full.
         * 
         * Add leafnode, link into chain
         * Progress back up map looking for node that is open
         * If a node is open, build indexes back down to the new leaf linking as you go
         * 
         * If we reach the root with no openenings, create another root and figure out
         * which branch to place the new key.
         * 
         *-----------------------------------------------------------------------------------*/
        private async Task<bool> IDXInsertLeafNodeRight(IDXCommand idxCmd)
        {
            AppIO.DebugLog(string.Format("Entering {0}", System.Reflection.MethodBase.GetCurrentMethod()!.Name));

            bool result = true;

            int idx = idxCmd.ID;
            int keyLen = idxCmd.Key.Length;
            int maxKeys = DbfInfo.IDX[idx].MaxKeys;

            byte[] keyBytes = idxCmd.Key;
            byte[] oldKey = new byte[keyLen];


            try
            {
                // -------------------------------------------------------------------------------------
                // If there is only one node, then we have a root/leaf node and need to
                // create a rood/index node and a leaf node before doing anything else
                // -------------------------------------------------------------------------------------
                if (idxCmd.NodeMap.Count < 2)
                {
                    // We have the root node which is also a leaf node
                    // which means we need to make a new root as a
                    // root/index and save the old root as a leaf node

                    // Get the current root node, change it to a leaf node
                    // give it a new position and write it out
                    IDXNode oldRoot = await IDXGetNode(idxCmd.NodeMap[0].Position, idx);
                    oldRoot.Attributes = 2;     // Convert it to a leaf node
                    oldRoot.Position = DbfInfo.IDX[idx].FileLen;    // New position
                    await IDXWrite(idx, oldRoot.Position, oldRoot.Buffer);

                    // Get the highest key from the (old root) leaf node
                    // for the first record of the new root node
                    oldKey = new byte[keyLen];
                    Array.Copy(oldRoot.Buffer, 12 + (maxKeys - 1) * (keyLen + 4), oldKey, 0, keyLen);
                    byte[] oldKeyPos = App.utl.RevInt2Bin(oldRoot.Position);

                    // Create the new root/index node that we'll use
                    // to overwrite the old root/leaf node
                    IDXNode newRoot = new()
                    {
                        Attributes = 1,
                        Position = 512
                    };

                    await IDXAppendKey(idx, newRoot, oldKey, oldRoot.Position);

                    // Update the node map so the (old root) leaf node is at
                    // the end like we're expecting with correct values
                    IDXNodePoint n = new()
                    {
                        Position = 1024,
                        Attributes = 2,
                        NodeRecord = idxCmd.NodeMap[0].NodeRecord
                    };

                    idxCmd.NodeMap.Add(n);
                    idxCmd.NodeMap[0].Attributes = 1;
                    idxCmd.NodeMap[0].NodeRecord = 0;   // Point at the original leaf node record

                }

                await IDXCreateIndexNodeRight(idxCmd, idxCmd.NodeMap.Count - 1, oldKey, idxCmd.Key, -1);
            }
            catch (Exception ex)
            {
                result = false;
                AppErrorHandling.SetError(8017, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return result;
        }

        /*
         * Here we're going to walk back up the node map, fixing the key
         * refernces as we go along.  
         * 
         * If we find an node with room, then we're done. 
         * 
         * If the index does not have room, then we create a new index 
         * node to the right and insert it into the chain.  Then move
         * up to the next node.
         * 
         * If we get to the root and there is no room, we'll add a new
         * root node before we finish up.
         * 
         * If newNodePos<0 then we expect to be in a leaf node
         * 
         * HEY STUPID!
         * TODO - when splitting, always put the top of the node into
         * the new leaf, that way we don't need to update the pointer to
         * this node in the node above! 
         */
        private async Task IDXCreateIndexNodeRight(IDXCommand idxCmd, int mapLevel, byte[] oldKey, byte[] newKey, int newNodePos)
        {
            AppIO.DebugLog("");
            AppIO.DebugLog(new string('~', 80));
            AppIO.DebugLog(string.Format("Entering {0}", System.Reflection.MethodBase.GetCurrentMethod()!.Name));

            if (mapLevel == 0)
                AppIO.DebugLog("At Root Level");

            if (AppErrorHandling.ErrorCount() == 0)
            {
                try
                {
                    int idx = idxCmd.ID;
                    int keyLen = newKey.Length;
                    int maxKeys = DbfInfo.IDX[idx].MaxKeys;

                    // Create a new node to the right and insert it into the chain
                    // Get the old node node
                    int leafPosition = idxCmd.NodeMap[mapLevel].Position;
                    IDXNode oldLeaf = await IDXGetNode(leafPosition, idx);
                    AppIO.DebugLog(string.Format("    Loading old leaf at {0} - keys={1}", leafPosition, oldLeaf.Keys));

                    if (oldLeaf.Keys < maxKeys)
                    {
                        // Node has room, so add to this node
                        AppIO.DebugLog($"    **** NODE HAS ROOM - inserting new key at {idxCmd.NodeMap[mapLevel].NodeRecord}****");

                        // New node always goes before old node
                        await IDXInsertKey(idx, oldLeaf, idxCmd.NodeMap[mapLevel].NodeRecord, newKey, newNodePos);
                    }
                    else
                    {
                        // Need to split this node, start by creating a new node
                        // with pointers set to become part of the leaf chain
                        // and it will be to the left of the old leaf
                        IDXNode newLeaf = new()
                        {
                            Attributes = oldLeaf.Attributes,
                            RightPtr = oldLeaf.Position,
                            LeftPtr = oldLeaf.LeftPtr,
                            Position = DbfInfo.IDX[idx].FileLen
                        };
                        AppIO.DebugLog(string.Format("    Creating new type {0} node {1}", newLeaf.Attributes, newLeaf.Position));

                        // If the right pointer of the old is >0 then we need to
                        // load and fix the left pointer of node to the right
                        if (oldLeaf.LeftPtr > 0)
                        {
                            IDXNode fixLink = await IDXGetNode(oldLeaf.LeftPtr, idx);
                            fixLink.RightPtr = newLeaf.Position;
                            AppIO.DebugLog(string.Format("    Fixing RightPtr at {0}", fixLink.RightPtr));
                            await IDXWrite(idx, fixLink.Position, fixLink.Buffer);
                        }

                        // Link the old leaf to the new leaf
                        oldLeaf.LeftPtr = newLeaf.Position;

                        AppIO.DebugLog(string.Format("    Chain is updated"));
                        AppIO.DebugLog(string.Format(""));

                        // The leaf chain has now been updated.  Now split the old
                        // leaf and put bottom half into the new leaf
                        int splitAt = 12 + maxKeys / 2 * (keyLen + 4);
                        int lastKeyByte = 12 + maxKeys * (keyLen + 4);
                        //int moveBlock = 12 + maxKeys * (keyLen + 4) - splitAt;
                        int moveBlock = lastKeyByte - splitAt;

                        byte[] oldLeafBuffer = oldLeaf.Buffer;
                        byte[] newLeafBuffer = newLeaf.Buffer;
                        byte[] clearBlock = new byte[500 - moveBlock];

                        // copy the top part of the old leaf to the top of the new leaf
                        Array.Copy(oldLeafBuffer, 12, newLeafBuffer, 12, splitAt - 12);

                        // Bring the bottom part of the old leaf to the top and clear the rest
                        Array.Copy(oldLeafBuffer, splitAt, oldLeafBuffer, 12, moveBlock);
                        Array.Copy(clearBlock, 0, oldLeafBuffer, moveBlock + 12, clearBlock.Length);

                        // update the node buffers
                        oldLeaf.Buffer = oldLeafBuffer;
                        newLeaf.Buffer = newLeafBuffer;

                        // Update the buffers with the corrected key count
                        int oldLeafKeys = maxKeys - maxKeys / 2;
                        oldLeaf.Keys = oldLeafKeys;
                        newLeaf.Keys = maxKeys - oldLeafKeys;
                        AppIO.DebugLog(string.Format("    Split @ {0}", oldLeafKeys));

                        int putRec = mapLevel == idxCmd.NodeMap.Count - 1 ? idxCmd.Record : newNodePos;

                        // Now add the new key to the correct leaf and write the other one
                        // This just tells us what leaf, not where in the leaf
                        if (idxCmd.NodeMap[mapLevel].NodeRecord < newLeaf.Keys)
                        {
                            // The new leaf has the top part of the old leaf
                            await IDXWrite(idx, oldLeaf.Position, oldLeaf.Buffer);

                            byte[] buffer = newLeaf.Buffer;
                            byte[] nodeRec = new byte[keyLen];
                            string newSKey = Encoding.UTF8.GetString(newKey);

                            // Put the new key into the old leaf
                            for (int i = 0; i < newLeaf.Keys; i++)
                            {
                                Array.Copy(buffer, 12 + i * (keyLen + 4), nodeRec, 0, keyLen);
                                string recSKey = Encoding.UTF8.GetString(nodeRec);
                                int keyCompare = StructuralComparisons.StructuralComparer.Compare(newKey, nodeRec);

                                AppIO.DebugLog(string.Format("    Comparing {0} to {1} = {2}", newSKey.Trim(), recSKey.Trim(), keyCompare));

                                if (keyCompare < 0)
                                {
                                    // Insert here
                                    AppIO.DebugLog(string.Format("    Inserting node type {0} at record {1}", buffer[0], i));
                                    await IDXInsertKey(idx, newLeaf, i, newKey, putRec);

                                    if (newLeaf.IsLeafNode)
                                    {
                                        // Update the final resting information
                                        //idxCmd.LeafNode = newLeaf.Position;
                                        //idxCmd.LeafRec = i;

                                        idxCmd.NodeMap[mapLevel].Position = newLeaf.Position;
                                        idxCmd.NodeMap[mapLevel].NodeRecord = i;
                                    }
                                    break;
                                }
                                else if (keyCompare > 0)
                                {
                                    if (i == oldLeaf.Keys - 1)
                                    {
                                        // Append and done
                                        AppIO.DebugLog(string.Format("    Appending node type {0}", buffer[0], i));
                                        await IDXAppendKey(idx, newLeaf, newKey, putRec);

                                        if (newLeaf.IsLeafNode)
                                        {
                                            // Update the final resting information
                                            //idxCmd.LeafNode = newLeaf.Position;
                                            //idxCmd.LeafRec = newLeaf.Keys - 1;

                                            idxCmd.NodeMap[mapLevel].Position = newLeaf.Position;
                                            idxCmd.NodeMap[mapLevel].NodeRecord = i;
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            await IDXWrite(idx, newLeaf.Position, newLeaf.Buffer);

                            byte[] buffer = oldLeaf.Buffer;
                            byte[] nodeRec = new byte[keyLen];
                            string newSKey = Encoding.UTF8.GetString(newKey);

                            // Put the new key into the new leaf
                            for (int i = 0; i < oldLeaf.Keys; i++)
                            {
                                Array.Copy(buffer, 12 + i * (keyLen + 4), nodeRec, 0, keyLen);
                                string recSKey = Encoding.UTF8.GetString(nodeRec);
                                int keyCompare = StructuralComparisons.StructuralComparer.Compare(newKey, nodeRec);

                                AppIO.DebugLog(string.Format("    Comparing {0} to {1} = {2}", newSKey.Trim(), recSKey.Trim(), keyCompare));

                                if (keyCompare < 0)
                                {
                                    // Insert here
                                    AppIO.DebugLog(string.Format("    Inserting node type {0} at record {1}", buffer[0], i));
                                    await IDXInsertKey(idx, oldLeaf, i, newKey, putRec);

                                    if (oldLeaf.IsLeafNode)
                                    {
                                        // Update the final resting information
                                        //idxCmd.LeafNode = oldLeaf.Position;
                                        //idxCmd.LeafRec = i;

                                        idxCmd.NodeMap[mapLevel].Position = oldLeaf.Position;
                                        idxCmd.NodeMap[mapLevel].NodeRecord = i;
                                    }
                                    break;
                                }
                                else if (keyCompare > 0)
                                {
                                    if (i == newLeaf.Keys - 1)
                                    {
                                        // Append and done
                                        AppIO.DebugLog(string.Format("    Appending node type {0}", buffer[0], i));
                                        await IDXAppendKey(idx, oldLeaf, newKey, newNodePos);

                                        if (oldLeaf.IsLeafNode)
                                        {
                                            // Update the final resting information
                                            //idxCmd.LeafNode = oldLeaf.Position;
                                            //idxCmd.LeafRec = oldLeaf.Keys - 1;

                                            idxCmd.NodeMap[mapLevel].Position = oldLeaf.Position;
                                            idxCmd.NodeMap[mapLevel].NodeRecord = i;
                                        }

                                        break;
                                    }
                                }
                            }
                        }

                        // Get the last record from the old and new leaves
                        if (mapLevel == 0)
                        {
                            if (oldLeaf.IsRootNode)
                            {
                                AppIO.DebugLog(string.Format("    Just split root"));
                                // We just split the root, so make the two
                                // nodes into index nodes and write them out
                                oldLeaf.Attributes = 0;
                                newLeaf.Attributes = 0;
                                await IDXWrite(idx, oldLeaf.Position, oldLeaf.Buffer);
                                await IDXWrite(idx, newLeaf.Position, newLeaf.Buffer);
                            }
                            else
                                throw new Exception("Index does not match table.  Recreate the index.");
                        }

                        // Get the last keys from the old and new leaves
                        oldKey = new byte[keyLen];
                        newKey = new byte[keyLen];
                        Array.Copy(oldLeaf.Buffer, 12 + (oldLeaf.Keys - 1) * (keyLen + 4), oldKey, 0, keyLen);
                        Array.Copy(newLeaf.Buffer, 12 + (newLeaf.Keys - 1) * (keyLen + 4), newKey, 0, keyLen);

                        if (mapLevel > 0)
                        {
                            // Walk up the map chain
                            await IDXCreateIndexNodeRight(idxCmd, mapLevel - 1, oldKey, newKey, newLeaf.Position);
                        }
                        else
                        {
                            // We just split the root, create a new root and put the
                            // two node references into it and then we're finished!
                            IDXNode rootNode = new()
                            {
                                Attributes = 1,
                                LeftPtr = -1,
                                RightPtr = -1,
                                Position = DbfInfo.IDX[idx].FileLen
                            };

                            AppIO.DebugLog(string.Format("    Creating new root at {0}", rootNode.Position));
                            AppIO.DebugLog(string.Format("    Adding {0}", Encoding.UTF8.GetString(oldKey)));
                            AppIO.DebugLog(string.Format("    Adding {0}", Encoding.UTF8.GetString(newKey)));

                            await IDXAppendKey(idx, rootNode, newKey, newLeaf.Position);
                            await IDXAppendKey(idx, rootNode, oldKey, oldLeaf.Position);
                            await IDXWrite(idx, rootNode.Position, rootNode.Buffer);

                            // Update the root node position and then get out
                            byte[] rootPos = Convert.FromBase64String(App.utl.MKI(rootNode.Position));
                            DbfInfo.IDX[idx].RootNode = rootNode.Position;
                            DbfInfo.IDX[idx].IDXStream!.Position = 0;
                            DbfInfo.IDX[idx].IDXStream!.Write(rootPos, 0, 4);
                            AppIO.DebugLog(string.Format("    Updated root node position to {0}", rootNode.Position));
                            DbfInfo.IDX[idx].IDXStream!.Flush();

                        }
                    }
                }
                catch (Exception ex)
                {
                    AppErrorHandling.SetError(8017, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                }
            }

            AppIO.DebugLog("---- INSERT RIGHT DONE ----");
            AppIO.DebugLog("");
            AppIO.DebugLog("");
        }



        /*-----------------------------------------------------------------------------------*
         *-----------------------------------------------------------------------------------*/
        private async Task<bool> IDXInsertKeyInNode(int idx, IDXCommand idxCmd)
        {
            bool result = true;

            try
            {
                byte[] keyBytes = idxCmd.Key;

                // Are we inserting into an index or leaf node?
                int recPos;
                int nodePosition;

                if (idxCmd.Command == 3)
                {
                    recPos = idxCmd.NodeMap[^1].NodeRecord;
                    nodePosition = idxCmd.NodeMap[^1].Position;
                }
                else
                {
                    recPos = idxCmd.NodeMap[^2].NodeRecord;
                    nodePosition = idxCmd.NodeMap[^2].Position;
                }

                IDXNode node = await IDXGetNode(nodePosition, idx);
                await IDXInsertKey(idx, node, recPos, keyBytes, idxCmd.Record);
            }
            catch (Exception ex)
            {
                result = false;
                AppErrorHandling.SetError(8017, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return result;

        }

        private async Task<bool> IDXInsertKey(int idx, IDXNode node, int atRec, byte[] keyBytes, int recNo)
        {
            bool result = true;

            try
            {
                byte[] buffer = node.Buffer;

                int keyLen = DbfInfo.IDX[idx].KeyLen;

                int insertAt = 12 + atRec * (keyLen + 4);
                int moveBlockTo = 12 + (atRec + 1) * (keyLen + 4);
                int blockLen = 12 + node.Keys * (keyLen + 4) - insertAt;

                if (insertAt < 0 || moveBlockTo < 0 || blockLen < 0 || keyLen < 0)
                    result = true;

                if (PRINTDEBUG)
                    AppIO.DebugLog(string.Format("    Insert @ {0}   move to {1}   len {2}   REC {3}", insertAt, moveBlockTo, blockLen, atRec));

                Array.Copy(buffer, insertAt, buffer, moveBlockTo, blockLen);                    // Move evertything down
                Array.Copy(keyBytes, 0, buffer, insertAt, keyLen);                              // Insert the key

                byte[] recBytes = App.utl.RevInt2Bin(recNo);
                Array.Copy(recBytes, 0, buffer, insertAt + keyLen, 4);                          // Insert the record/position

                recBytes = Convert.FromBase64String(App.utl.MKU((ushort)(node.Keys + 1)));
                Array.Copy(recBytes, 0, buffer, 2, 2);                                          // Update keycount

                node.Keys++;
                node.Buffer = buffer;

                if (await IDXWrite(idx, node.Position, buffer) == false)
                    throw new Exception("Failed to write IDX node");
            }
            catch (Exception ex)
            {
                result = false;
                AppErrorHandling.SetError(8017, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return result;

        }


        /*-----------------------------------------------------------------------------------*
         * Append this key to the end of the leaf pointed at by the node point
         * Return true successful, false if we need to add a new node
         *-----------------------------------------------------------------------------------*/
        private async Task<bool> IDXAppendKeyToNode(int idx, IDXCommand idxCmd, byte[] keyBytes, int recNo, int nodePoint)
        {
            bool NodeUpdated = true;

            try
            {
                IDXNode thisNode = await IDXGetNode(idxCmd.NodeMap[nodePoint].Position, idx);
                int MaxKeys = DbfInfo.IDX[idx].MaxKeys;

                if (thisNode.Keys < MaxKeys)
                {
                    await IDXAppendKey(idx, thisNode, keyBytes, recNo);   // Just append the key and save

                    // Update the final resting information
                    //idxCmd.LeafNode = thisNode.Position;
                    //idxCmd.LeafRec = thisNode.Keys - 1;
                }
                else
                    NodeUpdated = false;
            }
            catch (Exception ex)
            {
                NodeUpdated = false;
                AppErrorHandling.SetError(8017, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return NodeUpdated;
        }


        /*-----------------------------------------------------------------------------------*
         * Append a key to a node that is not full
         * Parameter thisNode is not expected to be updated
         *-----------------------------------------------------------------------------------*/
        private async Task<bool> IDXAppendKey(int idx, IDXNode thisNode, byte[] keyBytes, int recNo)
        {
            bool result = true;

            try
            {
                int keys = ++thisNode.Keys;
                int keyLen = keyBytes.Length;
                int position = 12 + (keys - 1) * (keyLen + 4);

                byte[] buffer = thisNode.Buffer;
                byte[] lbytes = App.utl.Long2Bin(keys);
                byte[] rbytes = App.utl.RevInt2Bin(recNo);

                Array.Copy(lbytes, 0, buffer, 2, 2);                    // Number of keys in this node
                Array.Copy(keyBytes, 0, buffer, position, keyLen);      // Sort key
                Array.Copy(rbytes, 0, buffer, position + keyLen, 4);    // Record number

                thisNode.Buffer = buffer;

                if (await IDXWrite(idx, thisNode.Position, buffer) == false)
                    throw new Exception("Failed to write IDX node");
            }
            catch (Exception ex)
            {
                result = false;
                AppErrorHandling.SetError(8018, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return result;
        }


        /*-----------------------------------------------------------------------------------*
         * DEBUG routine to display the complete node map
         *-----------------------------------------------------------------------------------*/
        public async Task IDXMapLinearDisplay(int idx, int position)
        {

            try
            {
                if (idx < DbfInfo.IDX.Count)
                {
                    IDXInfo idxInfo = DbfInfo.IDX[idx];
                    int keyLen = DbfInfo.IDX[idx].KeyLen;

                    AppIO.DebugLog("");
                    AppIO.DebugLog(new string('-', 80));
                    AppIO.DebugLog("Linear Map for " + DbfInfo.IDX[idx].FileName);
                    AppIO.DebugLog("");
                    AppIO.DebugLog(string.Format("Root Node at {0}", DbfInfo.IDX[idx].RootNode));
                    AppIO.DebugLog(string.Format("File Length Calculated {0}   File Length Actual {1}", DbfInfo.IDX[idx].FileLen, DbfInfo.IDX[idx].IDXStream!.Length));
                    AppIO.DebugLog(new string('-', 80));

                    while (position < DbfInfo.IDX[idx].FileLen)
                    {
                        IDXNode node = await IDXGetNode(position, idx);
                        await IDXMapNodeDebug(node, keyLen, position);
                        position += 512;
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(8019, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            AppIO.DebugLog(new string('-', 80));
            AppIO.DebugLog("");
        }

        public async Task IDXMapNodeDebug(IDXNode node, int keyLen, int position)
        {
            int keys = node.Keys;
            byte[] buffer = node.Buffer;

            byte[] thisKey = new byte[keyLen];
            byte[] recBytes = new byte[4];
            string nodeType = (node.IsRootNode ? "R" : string.Empty) + (node.IsIndexNode ? "I" : string.Empty) + (node.IsLeafNode ? "L" : string.Empty);

            AppIO.DebugLog("");
            AppIO.DebugLog(string.Format("Position {0}   type {1}   Left {2}   Right {3}   Keys {4}", position, nodeType, node.LeftPtr, node.RightPtr, node.Keys));
            AppIO.DebugLog(new string('-', 60));

            for (int j = 0; j < keys; j++)
            {
                int keyPos = 12 + j * (keyLen + 4);
                Array.Copy(buffer, keyPos, thisKey, 0, keyLen);
                Array.Copy(buffer, keyPos + keyLen, recBytes, 0, 4);
                int pos = App.utl.RevBin2Int(recBytes);
                AppIO.DebugLog(Encoding.UTF8.GetString(thisKey) + " " + pos.ToString());
            }

            AppIO.DebugLog("");
        }


        /*-----------------------------------------------------------------------------------*
         * DEPRECATED
         * Use the provided node map to change the "last key" for any node in the list
         * where the current key's last value is less than the key found in the parameter 
         * keyBytes up to but not including the StopAt map point
         *-----------------------------------------------------------------------------------*/
        private async Task IDXUpdateMap(int idx, IDXCommand idxCmd, int stopAt, byte[] keyBytes)
        {
            IDXInfo idxInfo = DbfInfo.IDX[idx];
            AppErrorHandling.ClearErrors();

            for (int i = 0; i < stopAt; i++)
            {
                // Get this node map record
                IDXNodePoint point = idxCmd.NodeMap[i];

                // Get the referenced node
                IDXNode node = await IDXGetNode(point.Position, idx);

                // Get the referenced record
                int keyPos = 12 + point.NodeRecord * (keyBytes.Length + 4);
                byte[] thisKey = new byte[keyBytes.Length];
                byte[] buffer = node.Buffer;
                Array.Copy(buffer, keyPos, thisKey, 0, keyBytes.Length);

                // Compare the keybytes parameter with the record key
                bool mustUpdate = false;
                for (int j = 0; j < keyBytes.Length; j++)
                {
                    if (keyBytes[j] > thisKey[j])
                    {
                        // keybytes is greater so must update the record
                        mustUpdate = true;
                        break;
                    }
                }

                // If we must update, then write the keybyte value to the record
                // and write the node back out to the index file
                if (mustUpdate)
                {
                    Array.Copy(keyBytes, 0, buffer, keyPos, keyBytes.Length);
                    if (await IDXWrite(idx, point.Position, buffer) == false)
                        throw new Exception("Failed to write IDX node");

                }
            }
        }

        /*-----------------------------------------------------------------------------------*
         * Write a node to  the indicated index with protection
         *-----------------------------------------------------------------------------------*/
        private async Task<bool> IDXWrite(int idx, int position, byte[] buffer)
        {
            bool llSuccess = true;
            AppErrorHandling.ClearErrors();

            try
            {
                if (position + buffer.Length > DbfInfo.IDX[idx].FileLen)
                {
                    DbfInfo.IDX[idx].FileLen = position + buffer.Length;

                    // Write the new file length to the header
                    byte[] flen = App.utl.Long2Bin(DbfInfo.IDX[idx].FileLen);
                    DbfInfo.IDX[idx].IDXStream!.Position = 8;
                    DbfInfo.IDX[idx].IDXStream!.Write(flen, 0, 4);
                }

                // Write the node to the file
                DbfInfo.IDX[idx].IDXStream!.Position = position;
                DbfInfo.IDX[idx].IDXStream!.Write(buffer, 0, buffer.Length);
                DbfInfo.IDX[idx].IDXStream!.Flush();

                // DEBUG
                IDXNode test = new() { Buffer = buffer };
                AppIO.DebugLog(string.Format("    Writing node for index {0} type {1} to position {2}, file length={3}, keys={4}, leftPtr={5}, rightPtr={6}", idx, buffer[0], position, DbfInfo.IDX[idx].FileLen, test.Keys, test.LeftPtr, test.RightPtr));
                await IDXMapNodeDebug(test, DbfInfo.IDX[idx].KeyLen, position);
            }
            catch (Exception ex)
            {
                llSuccess = false;
                AppErrorHandling.SetError(8018, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return llSuccess;
        }


        /*-----------------------------------------------------------------------------------*
         * Take the search key byte array sent and do the search.
         * 
         * Parameters:
         *      idx (int)           - int value of index in order opened
         *      searchValue (byte[])- byte array of search key
         *      rec (int)           - starting record
         *      ascending(bool)     - if true, search in normal order, otherwise
         *                            search in reverse order - TODO
         *      justFind(bool)      - if true, we're just seeking with not decision
         *                            on what to do with the key
         *-----------------------------------------------------------------------------------*/
        public async Task<IDXCommand> IDXSearchByKey(int idx, byte[] searchValue, int rec, bool naturalOrder, bool justFind)
        {
            AppIO.DebugLog(string.Format("Entering {0}", System.Reflection.MethodBase.GetCurrentMethod()!.Name));

            IDXCommand idxCmd;
            if (justFind) DbfInfo.Found = false;

            try
            {
                if (searchValue.Length == DbfInfo.IDX[idx].KeyLen)
                {
                    // Set up the command and start the search
                    idxCmd = new()
                    {
                        ID = idx,
                        Record = rec,
                        FindOnly = justFind,
                        Key = searchValue,
                        ReverseOrder = !naturalOrder
                    };

                    IDXNode node = await IDXGetNode(DbfInfo.IDX[idx].RootNode, idx);
                    await IDXFindFromRoot(node, idxCmd);

                    // Remember where the index record was located
                    DbfInfo.IDX[idx].RecordStatus = idxCmd;

                    if (PRINTDEBUG)
                        AppIO.DebugLog(string.Format("    ---- Command {0} ---- Position {1} ---- Record {2} ---- Node Record {3} ----", idxCmd.Command, idxCmd.NodeMap[^1].Position, idxCmd.Record, idxCmd.NodeMap[^1].NodeRecord));
                }
                else
                {
                    // Key length is incorrect
                    DbfInfo.IDX[idx].RecordStatus = new();
                    idxCmd = new()
                    {
                        Command = -99
                    };

                    AppErrorHandling.SetError(8019, "Search key is too long", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                }
            }
            catch (Exception ex)
            {
                idxCmd = new();
                AppErrorHandling.SetError(8019, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            if (justFind) DbfInfo.Found = idxCmd.Command == 1;
            return idxCmd;
        }

        /*-----------------------------------------------------------------------------------*
         * Take the value sent and convert it to a byte array representing the
         * data that is being searched.  Then search.
         * 
         * 2025-01-30 - VFP stores numbers and datetimes in a very weird fashion
         *              that defies a straight forward answer.  It appears to be
         *              some sort of function unlike how data is stored in the table.
         *              
         * 2025-03-06 - DOH!  The number is turned negative (so that negative numbers
         *              become positive, and then stored like the table, but with the
         *              least significan byte stored first.  All numeric values except
         *              for integer, dateonly, and datetime are stored as 8 byte
         *              keys using the DOUBLE type conversion logic.  Doing this makes
         *              everything work for sorting the key.
         *              
         * TODO - Need to get conversion finished and tested
         *-----------------------------------------------------------------------------------*/
        /// <summary>
        /// Perform search using object value instead of byte array
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="value"></param>
        /// <param name="rec"></param>
        /// <param name="ascending"></param>
        /// <param name="justFind"></param>
        /// <returns></returns>
        public async Task<IDXCommand> IDXSearch(int idx, object value, int rec, bool naturalOrder, bool justFind)
        {
            AppIO.DebugLog(string.Format("Entering {0}", System.Reflection.MethodBase.GetCurrentMethod()!.Name));

            IDXCommand idxCmd;
            AppErrorHandling.ClearErrors();

            try
            {
                string type = value.GetType().Name;
                byte[] searchValue = [.. Enumerable.Repeat<byte>(32, DbfInfo.IDX[idx].KeyLen)];
                byte[] objValue;

                switch (type.ToLower())
                {
                    case "string":      // length of string
                        objValue = Encoding.UTF8.GetBytes(value.ToString() ?? string.Empty);
                        break;

                    case "dateonly":    // 8 bytes
                        string dt = ((DateOnly)value).ToString("yyyyMMdd");
                        objValue = Encoding.ASCII.GetBytes(dt);
                        break;

                    case "boolean":     // 1 byte
                        searchValue = new byte[1];
                        objValue = new byte[1];
                        objValue[0] = (byte)((bool)value ? 0x46 : 0x54);
                        break;

                    case "int":
                    case "double":
                        throw new Exception(string.Format("11||Search for type {0} not implemented", type));

                    case "datetime":    // 8 bytes
                        throw new Exception(string.Format("11||Search for type {0} not implemented", type));

                    default:
                        throw new Exception(string.Format("11||Search for type {0} not implemented", type));
                }

                if (objValue.Length <= searchValue.Length)
                {
                    Array.Copy(objValue, 0, searchValue, 0, objValue.Length);

                    idxCmd = new()
                    {
                        ID = idx,
                        Record = rec,
                        FindOnly = justFind,
                        Key = searchValue,
                        ReverseOrder = naturalOrder
                    };

                    IDXNode node = await IDXGetNode(DbfInfo.IDX[idx].RootNode, idx);
                    await IDXFindFromRoot(node, idxCmd);

                    // Remember where the index record was located
                    DbfInfo.IDX[idx].RecordStatus = idxCmd;

                    if (PRINTDEBUG)
                        AppIO.DebugLog(string.Format("    ---- Command {0} ---- Position {1} ---- Record {2} ---- Node Record {3} ----", idxCmd.Command, idxCmd.NodeMap[^1].Position, idxCmd.Record, idxCmd.NodeMap[^1].NodeRecord));
                }
                else
                {
                    DbfInfo.IDX[idx].RecordStatus = new();
                    idxCmd = new()
                    {
                        Command = -99
                    };

                    AppErrorHandling.SetError(8019, "Search key is too long", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                }
            }
            catch (Exception ex)
            {
                idxCmd = new();
                AppErrorHandling.SetError(8019, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }


            return idxCmd;
        }


        /*-----------------------------------------------------------------------------------*
         * Remove a node record from the node and save it, returning the updated node
         * in the second parameter
         * 
         * Parameters
         *      idx     - index being written
         *      node    - current node from the index
         *      noderec - key record being removed
         *-----------------------------------------------------------------------------------*/
        private async Task<bool> IDXRemoveKey(int idx, IDXNode node, int nodeRec)
        {
            //AppIO.DebugLog(string.Format("Entering {0} - idx={1},  node.pos={2}, noderec={4}", System.Reflection.MethodBase.GetCurrentMethod()!.Name, idx, node.Position, nodeRec));
            AppErrorHandling.ClearErrors();
            bool success = true;

            try
            {
                int keyLen = DbfInfo.IDX[idx].KeyLen;
                byte[] buffer = node.Buffer;

                // Remove the key
                int removeAt = 12 + nodeRec * (keyLen + 4);
                int lastRec = 12 + node.Keys * (keyLen + 4);
                int moveBlock = lastRec - removeAt;
                Array.Copy(buffer, removeAt + keyLen + 4, buffer, removeAt, moveBlock);
                Array.Copy(new byte[keyLen], 0, buffer, lastRec, keyLen);

                // Update the node and write it out
                node.Buffer = buffer;
                node.Keys--;
                await IDXWrite(idx, node.Position, node.Buffer);
            }
            catch (Exception ex)
            {
                success = false;
                AppErrorHandling.SetError(8018, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return success;
        }


        /*-----------------------------------------------------------------------------------*
         * Update the key and save the node, returning the updated node in the
         * second parameter.
         * 
         * Parameters
         *      idx     - index being written
         *      node    - current node from the index
         *      noderec - key record being replaced
         *      keyBytes- array holding the key to be used in the replacement
         *-----------------------------------------------------------------------------------*/
        private async Task<bool> IDXReplaceKey(int idx, IDXNode node, int nodeRec, byte[] keyBytes)
        {
            AppIO.DebugLog(string.Format("Entering {0}", System.Reflection.MethodBase.GetCurrentMethod()!.Name));

            AppErrorHandling.ClearErrors();
            bool success;

            try
            {
                byte[] buffer = node.Buffer;
                int keyLen = DbfInfo.IDX[idx].KeyLen;

                if (keyBytes.Length == 0)
                {
                    // no key sent, so this is a remove, not a replace
                    success = await IDXRemoveKey(idx, node, nodeRec);
                }
                else
                {
                    // Update the key
                    Array.Copy(keyBytes, 0, buffer, 12 + nodeRec * (keyLen + 4), keyLen);

                    node.Buffer = buffer;
                    success = await IDXWrite(idx, node.Position, buffer);
                }
            }
            catch (Exception ex)
            {
                success = false;
                AppErrorHandling.SetError(8019, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return success;
        }


        /*-----------------------------------------------------------------------------------*
         * Update the index record with newKey.  If newKey.length==0 then we're
         * removing the record from the node.  If that record is the last record
         * then we'll recurse further up the chain.
         * 
         * Parameters
         *      idx     - index being adjusted
         *      idxCmd  - Index Command class with refernce information
         *      mapNode - What node are we adjusting?
         *      newKey  - key to insert in it's place (or remove if len=0)
         *      
         * Returns bool indicating success
         *-----------------------------------------------------------------------------------*/
        private async Task<bool> IDXUpdateIndexNode(int idx, IDXCommand idxCmd, int mapNode, byte[] newKey)
        {
            AppIO.DebugLog(string.Format("Entering {0}", System.Reflection.MethodBase.GetCurrentMethod()!.Name));

            AppErrorHandling.ClearErrors();
            bool success = true;

            try
            {
                int pos = idxCmd.NodeMap[^1].Position;
                int rec = idxCmd.NodeMap[^1].NodeRecord;
                int keyLen = DbfInfo.IDX[idx].KeyLen;
                byte[] keyBytes = new byte[DbfInfo.IDX[idx].KeyLen];
                byte[] oldKey = idxCmd.Key;

                if (pos > 0 && rec >= 0)
                {
                    // Get the node
                    IDXNode node = await IDXGetNode(pos, idx);

                    // update/remove the node record
                    await IDXReplaceKey(idx, node, rec, newKey);

                    if (node.Keys == 0) // Add this node to the free nodes chain
                    {
                        // If the node is empty, get the first lost key node
                        // if there are none, then this is the first
                        int freeNode = DbfInfo.IDX[idx].FreeNode;

                        if (DbfInfo.IDX[idx].FreeNode < 512)
                        {
                            node.LeftPtr = -1;
                            node.RightPtr = -1;
                            await IDXWrite(idx, pos, node.Buffer);
                        }
                        else
                        {
                            IDXNode FirstFree = await IDXGetNode(DbfInfo.IDX[idx].FreeNode, idx);

                            FirstFree.LeftPtr = pos;
                            node.RightPtr = FirstFree.Position;
                            node.LeftPtr = -1;

                            // Save the node
                            await IDXWrite(idx, FirstFree.Position, FirstFree.Buffer);
                            await IDXWrite(idx, pos, node.Buffer);
                        }

                        // Update the header
                        byte[] newFreeNodePos = App.utl.RevInt2Bin(pos);
                        DbfInfo.IDX[idx].FreeNode = pos;
                        DbfInfo.IDX[idx].IDXStream!.Position = 4;
                        DbfInfo.IDX[idx].IDXStream!.Write(newFreeNodePos);

                        success = await IDXUpdateIndexNode(idx, idxCmd, mapNode - 1, newKey);
                    }
                    else
                    {
                        // If it's the past or is the last key, we'll need to fix the index
                        if (rec >= node.Keys - 1)
                        {
                            Array.Copy(node.Buffer, 12 + (node.Keys - 1) * (keyLen + 4), keyBytes, 0, keyLen);
                            success = await IDXUpdateIndexNode(idx, idxCmd, mapNode - 1, keyBytes);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                AppErrorHandling.SetError(8018, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return success;
        }



        /*-----------------------------------------------------------------------------------*
         * Load a node from the index at the specified position and break it up into 
         * the structure for use
         *-----------------------------------------------------------------------------------*/
        private async Task<IDXNode> IDXGetNode(int position, int indexID)
        {
            AppErrorHandling.ClearErrors();
            IDXNode node = new();

            try
            {
                int keyLen = DbfInfo.IDX[indexID].KeyLen;
                //DbfInfo.IDX[indexID].Position = position;
                //DbfInfo.IDX[indexID].NodeRecord = 0;

                byte[] buffer = new byte[512];
                byte[] data2 = new byte[2];
                byte[] data4 = new byte[4];
                byte[] keyRec = new byte[keyLen];

                // Get the root node
                if (position + 512 <= DbfInfo.IDX[indexID].FileLen)
                {
                    DbfInfo.IDX[indexID].IDXStream!.Position = position;
                    DbfInfo.IDX[indexID].IDXStream!.ReadExactly(buffer);

                    node.Buffer = buffer;
                    node.Position = position;
                    node.ID = indexID;

                    AppIO.DebugLog(string.Format("    Read node at {0}, type {1}, keys {2}, leftPtr {3}, rightPtr {4}", node.Position, node.Attributes, node.Keys, node.LeftPtr, node.RightPtr));
                }
                else
                {
                    throw new Exception("Node reference is past end of file");
                }
            }
            catch (Exception ex)
            {
                node = new();
                AppErrorHandling.SetError(8019, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return node;
        }



        /*
         * Goto a record
         */
        private async Task<IDXCommand> IDXGoto(bool top, int idx)
        {
            AppIO.DebugLog(string.Format("Entering {0}", System.Reflection.MethodBase.GetCurrentMethod()!.Name));
            AppErrorHandling.ClearErrors();

            int keyLen = DbfInfo.IDX[idx].KeyLen;

            IDXCommand idxCmd = new()
            {
                ID = idx
            };

            int pos = DbfInfo.IDX[idx].RootNode;
            byte[] data4 = new byte[4];
            idxCmd.Key = new byte[keyLen];

            while (true)
            {
                // Get the root node
                IDXNode node = await IDXGetNode(pos, idx);
                IDXNodePoint point = new()
                {
                    Position = node.Position,
                    Attributes = node.Attributes
                };

                idxCmd.NodeMap.Add(point);

                // It's in this leaf, record it
                int k = top ? 0 : node.Keys - 1;
                int keyStart = 12 + k * (keyLen + 4);
                Array.Copy(node.Buffer, keyStart + keyLen, data4, 0, 4);    // position
                Array.Copy(node.Buffer, keyStart, idxCmd.Key, 0, keyLen);   // current key

                //DbfInfo.IDX[idx].NodeRecord = k;
                //DbfInfo.IDX[idx].Position = node.Position;

                idxCmd.Record = App.utl.RevBin2Int(data4);
                //idxCmd.NodeRecord = k;                                              // node record used
                idxCmd.Command = 1;

                if (node.IsLeafNode)
                    break;
                else
                    pos = idxCmd.Record;
            }

            DbfInfo.IDX[idx].RecordStatus = idxCmd;
            return idxCmd;
        }


        /*-----------------------------------------------------------------------------------*
         * Start with this Root node and look for a specific record in the index by 
         * recursing through the index nodes and counting off how many keys are in each 
         * leaf node until we get to the requested position.
         * 
         * Commands
         *   20 = Create a new index left
         *   21 = Create a new index right
         *   30 = Create a new root left
         *   31 = Create a new root right
         *   32 = Add a new index node below
         *   33 = Add a new leaf node below
         *   
         *   TODO -  There is no reason to iterate through every record if keys>9
         *               Jump to middle record
         *               Jump to 1/4 or 3/4 depending on result
         *               Iterate through the section pointed at by the result
         *-----------------------------------------------------------------------------------*/
        private async Task IDXFindFromRoot(IDXNode thisNode, IDXCommand idxCmd)
        {
            AppIO.DebugLog(string.Format("Entering {0}", System.Reflection.MethodBase.GetCurrentMethod()!.Name));
            AppErrorHandling.ClearErrors();

            try
            {
                byte[] key = idxCmd.Key;
                int RecNo = idxCmd.Record;
                bool justFind = idxCmd.FindOnly;

                if (PRINTDEBUG) AppIO.DebugLog("    Start of Search");

                // Record the root's node point
                IDXNodePoint point = new()
                {
                    Attributes = thisNode.Attributes,
                    Position = thisNode.Position
                };

                idxCmd.NodeMap.Insert(0, point);

                if (thisNode.IsRootNode == false && thisNode.IsLeafNode == false)
                    throw new Exception(string.Format("Expected Root Node at postion {0} (has attribute {1})", thisNode.Position, thisNode.Attributes));  // Error, expected Root node

                await IDXFindFromNode(thisNode, idxCmd);

                // Map is currently leaf to root, so
                // reverse it to be root to Leaf
                idxCmd.NodeMap.Reverse();

                AppIO.DebugLog(string.Format("idxCmd Command={0},  Rec={1},  Node Rec={2}", idxCmd.Command, idxCmd.Record, idxCmd.NodeMap[^1].NodeRecord));
                for (int i = 0; i < idxCmd.NodeMap.Count; i++)
                    AppIO.DebugLog(string.Format("Node Map {0} - Pos={1}  Node Rec={2}", i, idxCmd.NodeMap[i].Position, idxCmd.NodeMap[i].NodeRecord));

                // Store the idxCmd in the index record in case we need it later
                if (thisNode.ID >= 0)
                    DbfInfo.IDX[thisNode.ID].RecordStatus = idxCmd;
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(8019, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }
        }


        /*-----------------------------------------------------------------------------------*
         * Start with the root node and move down through the index nodes to find
         * the leaf node that should hold the value for which you seek.
         *-----------------------------------------------------------------------------------*/
        private async Task IDXFindFromNode(IDXNode thisNode, IDXCommand idxCmd)
        {
            AppIO.DebugLog(string.Format("Entering {0} - nodemap index = {1}   node pos={2}", System.Reflection.MethodBase.GetCurrentMethod()!.Name, idxCmd.NodeMap.Count - 1, thisNode.Position));

            AppErrorHandling.ClearErrors();

            try
            {
                int keyLen = DbfInfo.IDX[thisNode.ID].KeyLen;
                int maxKeys = DbfInfo.IDX[thisNode.ID].MaxKeys;

                byte[] data4 = new byte[4];
                byte[] lastKey = new byte[keyLen];
                byte[] keyRec = new byte[keyLen];

                if (thisNode.IsLeafNode == false)
                {
                    // This root nodes are the top index nodes
                    if (PRINTDEBUG)
                        AppIO.DebugLog(string.Format("    Searching for ={0}", Encoding.UTF8.GetString(idxCmd.Key)));

                    if (thisNode.Keys < 1)
                    {
                        idxCmd.Command = 2;                     // Add to this node since there are no keys
                        //idxCmd.Position = thisNode.Position;
                    }
                    else
                    {
                        // Look at the last key of this node
                        int lastKeyPosition = 12 + (thisNode.Keys - 1) * (keyLen + 4);
                        AppIO.DebugLog(string.Format(" Step 4 thisNode.Keys={0},   lastKeyPos={1}", thisNode.Keys, lastKeyPosition));
                        Array.Copy(thisNode.Buffer, lastKeyPosition, lastKey, 0, keyLen);
                        Array.Copy(thisNode.Buffer, lastKeyPosition + keyLen, data4, 0, 4);
                        //int nextNodePosition = Utilities.CVI(Convert.ToBase64String(data4));
                        int nextNodePosition = App.utl.RevBin2Int(data4);
                        int keyCompare = StructuralComparisons.StructuralComparer.Compare(idxCmd.Key, lastKey);

                        if (PRINTDEBUG)
                            AppIO.DebugLog(string.Format("    Record compare={0}   Last key = {1}", keyCompare, Encoding.ASCII.GetString(lastKey)));

                        if (keyCompare <= 0)
                        {
                            // Key is less than last key so we have the correct index node
                            Array.Copy(thisNode.Buffer, 12, lastKey, 0, keyLen);
                            keyCompare = StructuralComparisons.StructuralComparer.Compare(idxCmd.Key, lastKey);

                            if (PRINTDEBUG) AppIO.DebugLog(string.Format("    Correct index node"));

                            // This is the correct node. Search key belongs in this node
                            // Step through each index record to find the correct one
                            for (int j = 0; j < thisNode.Keys; j++)
                            {
                                //DbfInfo.IDX[thisNode.ID].NodeRecord = j;

                                int keyStart = 12 + j * (keyLen + 4);
                                Array.Copy(thisNode.Buffer, keyStart, keyRec, 0, keyLen);
                                Array.Copy(thisNode.Buffer, keyStart + keyLen, data4, 0, 4);
                                //nextNodePosition = Utilities.CVI(Convert.ToBase64String(data4));
                                nextNodePosition = App.utl.RevBin2Int(data4);
                                keyCompare = StructuralComparisons.StructuralComparer.Compare(idxCmd.Key, keyRec);

                                if (PRINTDEBUG)
                                    AppIO.DebugLog(string.Format("    Record compare={0}   Key = {1}", keyCompare, Encoding.ASCII.GetString(keyRec)));

                                idxCmd.NodeMap[0].NodeRecord = j;

                                // If we find the correct record, then call for it
                                if (keyCompare < 0) break;
                            }
                        }
                        else
                        {
                            // Enter last node until we hit the leaf
                            AppIO.DebugLog(string.Format("    Using last node reference"));
                            idxCmd.NodeMap[0].NodeRecord = thisNode.Keys - 1;
                            if (thisNode.Keys > 6) AppIO.DebugLog(string.Format("4 nodeRecord={0}", thisNode.Keys));
                        }

                        IDXNode nextNode = await IDXGetNode(nextNodePosition, thisNode.ID);

                        // Record the root's node point
                        IDXNodePoint point = new()
                        {
                            Attributes = nextNode.Attributes,
                            Position = nextNode.Position
                        };

                        AppIO.DebugLog(string.Format("    Going to node at {0}", nextNodePosition));
                        idxCmd.NodeMap.Insert(0, point);
                        await IDXFindFromNode(nextNode, idxCmd);
                    }
                }
                else
                {
                    // It's a leaf node so we're at the end of the chain
                    if (PRINTDEBUG) AppIO.DebugLog(string.Format("    Found a Leaf Node at Position ={0}", thisNode.Position));
                    await IDXFindFromLeaf(thisNode, idxCmd);
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(8019, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }
        }


        /*-----------------------------------------------------------------------------------*
         * Update zero based record of leaf node with dbf record position
         *----------------------------------------------------------------------------------*/
        private async Task IDXUpdateRecPos(IDXNode node, int nodeRec, int dbfRecord)
        {
            int idx = node.ID;
            int keyLen = DbfInfo.IDX[idx].KeyLen;

            if (nodeRec < node.Keys && nodeRec >= 0)
            {
                int pos = nodeRec * 12 + nodeRec * (keyLen + 4) + keyLen;
                byte[] recNo = App.utl.RevInt2Bin(dbfRecord);
                byte[] buffer = node.Buffer;
                Array.Copy(recNo, 0, buffer, pos, 4);
                node.Buffer = buffer;
            }
            else
                throw new Exception("Invalid node record");
        }


        /*-----------------------------------------------------------------------------------*
         * Starting at the first leaf node, all records greater than the starting record
         * will be updated +1 in order to correct the index after a record is inserted
         *----------------------------------------------------------------------------------*/
        private async Task IDXUpdateRecPosPlusOne(int idx, int startingRecord)
        {
            // sanity check
            if (startingRecord < DbfInfo.RecCount - 1)
            {
                try
                {
                    // Wasn't the last record, so find the first leaf
                    int pos = DbfInfo.IDX[idx].RootNode;
                    bool foundRoot = false;
                    IDXNode node = await IDXGetNode(pos, idx);
                    int keyLen = DbfInfo.IDX[idx].KeyLen;
                    byte[] key = new byte[keyLen];
                    byte[] lastKey = new byte[keyLen];
                    byte[] posBytes = new byte[4];

                    while (true)
                    {
                        if (node.IsLeafNode)
                            break;

                        // Get the first record
                        Array.Copy(node.Buffer, 0, key, 12, keyLen);
                        Array.Copy(node.Buffer, keyLen, posBytes, 4, 4);

                        // prevent an infinite loop with a corrupted index
                        if (node.IsRootNode == false)
                        {
                            // This new node's first key must be less than or equal
                            // to the last key, otherwise the index is corrupted
                            int keyCompare = StructuralComparisons.StructuralComparer.Compare(key, lastKey);
                            if (keyCompare <= 0)
                            {
                                // Remember this key and get the position of the next node
                                lastKey = [.. key];
                                pos = App.utl.RevBin2Int(posBytes);
                            }
                            else
                                throw new Exception("Index is corrupted");
                        }
                        else
                        {
                            if (foundRoot) throw new Exception("Index is courrupted");
                            foundRoot = true;
                        }

                        // Goto this next node
                        node = await IDXGetNode(pos, idx);
                    }

                    if (node.IsLeafNode && node.LeftPtr < 0)
                    {
                        while (true)
                        {
                            byte[] buffer = node.Buffer;


                            // Remember the first key of this node
                            byte[] firstKey = new byte[keyLen];
                            Array.Copy(buffer, 12, firstKey, 0, keyLen);

                            if (node.LeftPtr > 0)
                            {
                                // Check to make sure the last key is
                                // less than the first key of this node
                                int keyCompare = StructuralComparisons.StructuralComparer.Compare(firstKey, lastKey);
                                if (keyCompare < 0)
                                    throw new Exception("Index is corrupted"); // it's less than!
                            }

                            // This is the first leaf node.  Now we step
                            // through this leaf, adding 1 to  any record
                            // that is greater than the starting record
                            bool changed = false;
                            for (int i = 0; i < node.Keys; i++)
                            {
                                Array.Copy(buffer, 12 + i * (keyLen + 4), key, 0, keyLen);
                                Array.Copy(buffer, 12 + i * (keyLen + 4) + keyLen, posBytes, 0, 4);
                                pos = App.utl.RevBin2Int(posBytes);

                                if (pos > startingRecord)
                                {
                                    // Found one, update it
                                    posBytes = App.utl.RevInt2Bin(pos);
                                    Array.Copy(posBytes, 0, buffer, 12 + i * (keyLen + 4) + keyLen, 4);
                                    changed = true;
                                }
                            }

                            // If something was changd, write it back out
                            if (changed)
                                await IDXWrite(idx, node.Position, buffer);

                            // update the lastkey with this node's first key
                            lastKey = [.. firstKey];

                            // Now go to the next node to the right if
                            // the right pointer is greater than zero
                            // and make sure it's a leaf and has the
                            // left poionter > 0
                            if (node.RightPtr > 0)
                            {
                                node = await IDXGetNode(node.RightPtr, idx);
                                if (node.IsLeafNode == false || node.LeftPtr < 0)
                                    throw new Exception("Index is corrupted");
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Index is corrupted");
                    }
                }
                catch (Exception ex)
                {
                    AppErrorHandling.SetError(8018, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                }
            }
        }

        /*-----------------------------------------------------------------------------------*
         * Last step in the seek.  Try to find the key/record combination and if you
         * can't, return probably should be done to fix it.
         * 
         *   1 = Found key at this node's position and record
         *   2 = Append to this leaf
         *   3 = Insert to this leaf at RecNo
         *   4 = Create a new leaf to the left and split at
         *   5 = Create a new leaf to the right and split at
         *   9 = Found near match
         *  20 = Create a new index left
         *  21 = Create a new index right
         *  30 = Create a new root left
         *  31 = Create a new root right
         *  32 = Add a new index node below
         *  33 = Add a new leaf node below
         *
         *  -1 = Record is not and should be part of this index (index is out of date)
         * -99 = Error encountered
         *-----------------------------------------------------------------------------------*/
        private async Task IDXFindFromLeaf(IDXNode thisNode, IDXCommand idxCmd)
        {
            AppErrorHandling.ClearErrors();

            try
            {
                int RecNo = 0;
                int idx = thisNode.ID;

                if (PRINTDEBUG) AppIO.DebugLog(string.Format("    Leaf Search for {0}", Encoding.UTF8.GetString(idxCmd.Key)));

                int keyLen = DbfInfo.IDX[idx].KeyLen;
                byte[] keyRec = new byte[DbfInfo.IDX[idx].KeyLen];
                byte[] data4 = new byte[4];

                // Is it an index node?
                if (thisNode.IsLeafNode)
                {
                    // This root node is an index node that may lead to another index node below
                    if (PRINTDEBUG)
                        AppIO.DebugLog(string.Format("    Searching for ={0}", Encoding.UTF8.GetString(idxCmd.Key)));

                    if (thisNode.Keys > 0)
                    {
                        int keyStart = 12 + (thisNode.Keys - 1) * (keyLen + 4);
                        Array.Copy(thisNode.Buffer, keyStart, keyRec, 0, keyLen);
                        Array.Copy(thisNode.Buffer, keyStart + keyLen, data4, 0, 4);
                        //RecNo = Utilities.CVI(Convert.ToBase64String(data4));
                        RecNo = App.utl.RevBin2Int(data4);
                        bool insertHere = false;

                        int keyCompare = StructuralComparisons.StructuralComparer.Compare(idxCmd.Key, keyRec);

                        if (keyCompare > 0)
                        {
                            // The key is greater than the highest value record in this leaf
                            if (thisNode.Keys < DbfInfo.IDX[thisNode.ID].MaxKeys)
                            {
                                // We have room on this leaf node, find the right spot
                                if (PRINTDEBUG)
                                    AppIO.DebugLog(string.Format("    We have room on this node"));
                                insertHere = true;
                            }
                            else
                            {
                                // We have no room to add to this leaf
                                if (thisNode.RightPtr > 0)
                                {
                                    // We've reached the end so need to add to the right
                                    if (PRINTDEBUG)
                                    {
                                        AppIO.DebugLog(string.Format("    lastRec={0} => {1} - moving right", Encoding.UTF8.GetString(keyRec).Trim(), RecNo));
                                        AppIO.DebugLog(string.Format("    Going to position {0}", thisNode.RightPtr));
                                    }

                                    // too low - need to go right
                                    IDXNode nextNode = await IDXGetNode(thisNode.RightPtr, idx);

                                    idxCmd.NodeMap[0].Position = nextNode.Position;
                                    idxCmd.NodeMap[0].Attributes = nextNode.Attributes;

                                    await IDXFindFromLeaf(nextNode, idxCmd);
                                }
                                else
                                {
                                    // We're at the end of the index, so add another leaf right
                                    //idxCmd.Position = thisNode.Position;
                                    idxCmd.Command = 5;
                                    //idxCmd.NodeRecord = -1;  // Append to new node
                                }
                            }
                        }
                        else
                        {
                            if (PRINTDEBUG)
                            {
                                AppIO.DebugLog("");
                                AppIO.DebugLog(string.Format("    Correct leaf"));
                            }

                            if (thisNode.Keys < DbfInfo.IDX[idx].MaxKeys)
                                insertHere = true;
                            else
                            {
                                if (idxCmd.FindOnly)
                                {
                                    for (int k = 0; k < thisNode.Keys; k++)
                                    {
                                        keyStart = 12 + k * (keyLen + 4);
                                        Array.Copy(thisNode.Buffer, keyStart, keyRec, 0, keyLen);
                                        Array.Copy(thisNode.Buffer, keyStart + keyLen, data4, 0, 4);
                                        //RecNo = Utilities.CVI(Convert.ToBase64String(data4));
                                        RecNo = App.utl.RevBin2Int(data4);
                                        keyCompare = StructuralComparisons.StructuralComparer.Compare(idxCmd.Key, keyRec);

                                        if (PRINTDEBUG)
                                            AppIO.DebugLog(string.Format("    {0} <{1}> {2}", Encoding.UTF8.GetString(idxCmd.Key), keyCompare, Encoding.UTF8.GetString(keyRec)));

                                        if (keyCompare == 0)
                                        {
                                            // FOUND IT!
                                            idxCmd.Command = 1;     // Exact match
                                            idxCmd.NodeMap[0].NodeRecord = k;
                                            AppIO.DebugLog(string.Format("FOUND at record {0} in leaf node at position {1}", k, thisNode.Position));
                                        }

                                        if (keyCompare < 0)
                                        {
                                            if (App.CurrentDS.JaxSettings.Near)
                                            {
                                                // TOO FAR!
                                                idxCmd.Command = 9;     // NEAR MATCH
                                                idxCmd.NodeMap[0].NodeRecord = k;
                                                AppIO.DebugLog(string.Format("NEAR found at record {0} in leaf node at position {1}", k, thisNode.Position));
                                            }
                                            else
                                            {
                                                if (RecNo > 0)
                                                {
                                                    // Go past bottom to set EOF = .T.
                                                    await DBFGotoRecord("bottom");
                                                    await DBFSkipRecord(1);
                                                }
                                            }
                                            break;
                                        }
                                    }

                                }
                                else
                                {
                                    // Need to perform a split and append
                                    // Found the record where we need to split
                                    //idxCmd.Position = thisNode.Position;
                                    idxCmd.Command = 5;     // Split to the right
                                                            //idxCmd.NodeRecord = 0;  // Start a brand new node and append

                                    for (int k = 0; k < thisNode.Keys; k++)
                                    {
                                        keyStart = 12 + k * (keyLen + 4);
                                        Array.Copy(thisNode.Buffer, keyStart, keyRec, 0, keyLen);
                                        Array.Copy(thisNode.Buffer, keyStart + keyLen, data4, 0, 4);
                                        //RecNo = Utilities.CVI(Convert.ToBase64String(data4));
                                        RecNo = App.utl.RevBin2Int(data4);
                                        keyCompare = StructuralComparisons.StructuralComparer.Compare(idxCmd.Key, keyRec);

                                        if (PRINTDEBUG)
                                            AppIO.DebugLog(string.Format("    {0} <{1}> {2}", Encoding.UTF8.GetString(idxCmd.Key), keyCompare, Encoding.UTF8.GetString(keyRec)));

                                        if (keyCompare < 0)
                                        {
                                            //idxCmd.NodeRecord = k;  // Split here and append
                                            idxCmd.NodeMap[0].NodeRecord = k;
                                            AppIO.DebugLog(string.Format("Split at {0} in leaf node at position {1}", k, thisNode.Position));
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (insertHere)
                        {
                            AppIO.DebugLog("Insert here");

                            // We have the correct leaf, step through
                            // and get the correct record
                            for (int k = 0; k < thisNode.Keys; k++)
                            {
                                keyStart = 12 + k * (keyLen + 4);
                                Array.Copy(thisNode.Buffer, keyStart, keyRec, 0, keyLen);
                                Array.Copy(thisNode.Buffer, keyStart + keyLen, data4, 0, 4);
                                //RecNo = Utilities.CVI(Convert.ToBase64String(data4));
                                RecNo = App.utl.RevBin2Int(data4);
                                keyCompare = StructuralComparisons.StructuralComparer.Compare(idxCmd.Key, keyRec);

                                if (PRINTDEBUG)
                                    AppIO.DebugLog(string.Format("    {0} <{1}> {2}", Encoding.UTF8.GetString(idxCmd.Key), keyCompare, Encoding.UTF8.GetString(keyRec)));

                                if (keyCompare == 0)
                                {
                                    // This is a match
                                    if (RecNo == idxCmd.Record || idxCmd.Record == 0)
                                    {
                                        // Found the exact record or first match (if idxCmd.Record==0)
                                        //DbfInfo.IDX[thisNode.ID].NodeRecord = k;
                                        idxCmd.NodeMap[0].NodeRecord = k;

                                        //idxCmd.Position = thisNode.Position;
                                        //idxCmd.NodeRecord = k;

                                        if (idxCmd.Record > 0)
                                        {
                                            // Found exact match
                                            idxCmd.Command = 1;
                                        }
                                        else
                                        {
                                            // Found near match
                                            idxCmd.Record = RecNo;
                                            idxCmd.Command = 9;
                                        }
                                    }
                                    else
                                    {
                                        if (idxCmd.NodeMap[0].Position == 0)
                                        {
                                            // Only want position of first match recorded at this point
                                            idxCmd.NodeMap[0].Position = thisNode.Position;
                                            idxCmd.NodeMap[0].NodeRecord = k;
                                        }

                                        if (PRINTDEBUG)
                                            AppIO.DebugLog("   Found but recno is not same");

                                        //DbfInfo.IDX[thisNode.ID].NodeRecord = k;
                                        idxCmd.NodeMap[0].NodeRecord = k;
                                        idxCmd.Command = 1;     // Found key match
                                    }

                                    int vrecPos = k + 1;
                                    while (thisNode.LeftPtr > 0)
                                    {
                                        thisNode = await IDXGetNode(thisNode.LeftPtr, idx);
                                        vrecPos += thisNode.Keys;
                                    }

                                    if (PRINTDEBUG) AppIO.DebugLog(string.Format("   Found it!  Rec={0}, VRec={1}", RecNo, vrecPos));
                                }
                                else if (keyCompare <= 0)
                                {
                                    if (idxCmd.FindOnly)
                                    {
                                        if (App.CurrentDS.JaxSettings.Near)
                                        {
                                            if (PRINTDEBUG) AppIO.DebugLog("   NEAR!");

                                            // Near is set on, so select this record
                                            idxCmd.Record = RecNo;
                                            //idxCmd.Position = thisNode.Position;
                                            //idxCmd.NodeRecord = k;
                                            idxCmd.NodeMap[0].NodeRecord = k;
                                            idxCmd.Command = 9;     // Found a NEAR match
                                            //DbfInfo.IDX[thisNode.ID].NodeRecord = k;

                                            int vrecPos = k + 1;
                                            while (thisNode.LeftPtr > 0)
                                            {
                                                thisNode = await IDXGetNode(thisNode.LeftPtr, idx);
                                                vrecPos += thisNode.Keys;
                                            }

                                            if (PRINTDEBUG) AppIO.DebugLog(string.Format("   Found it!  Rec={0}, VRec={1}", RecNo, vrecPos));
                                        }
                                        else
                                        {
                                            if (PRINTDEBUG) AppIO.DebugLog("   Where is it?");

                                            // Can't find it
                                            //idxCmd.Position = thisNode.Position;
                                            //idxCmd.NodeRecord = k;
                                            idxCmd.NodeMap[0].NodeRecord = k;
                                            idxCmd.Command = -1;
                                            //DbfInfo.IDX[thisNode.ID].NodeRecord = -1;
                                        }
                                    }
                                    else
                                    {
                                        // Insert at this record
                                        //idxCmd.Position = thisNode.Position;
                                        //idxCmd.NodeRecord = k;
                                        idxCmd.NodeMap[0].NodeRecord = k;
                                        idxCmd.Command = 3;
                                        //DbfInfo.IDX[thisNode.ID].NodeRecord = k;
                                        if (PRINTDEBUG) AppIO.DebugLog(string.Format("    Please insert @ {0}", k));
                                    }
                                }

                                if (idxCmd.Command != 0)
                                    break;
                            }
                        }
                    }

                    if (idxCmd.Command == 0 && idxCmd.FindOnly == false)
                    {
                        // No find, so what do we do with this leaf?
                        if (thisNode.Keys < DbfInfo.IDX[idx].MaxKeys)
                            idxCmd.Command = 2;     // Append to this leaf
                        else
                            idxCmd.Command = 5;     // Ran out of keys, so split this leaf node

                        idxCmd.NodeMap[0].NodeRecord = 0;
                    }
                }
                else
                {
                    // Error, expected index node
                    throw new Exception(string.Format("Expected Leaf Node at postion {0} (has attribute {1})", thisNode.Position, thisNode.Attributes));
                }
            }
            catch (Exception ex)
            {
                idxCmd.Command = -99;
                AppErrorHandling.SetError(8019, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }
        }



        /*-----------------------------------------------------------------------------------*
         * Peform a record skip using the index to decide what record to land on
         * by moving through the leaf nodes starting with the last location.
         *-----------------------------------------------------------------------------------*/
        private async Task<IDXCommand> IDXSkipRecord(int recSkip, int thisIDX)
        {
            AppErrorHandling.ClearErrors();
            int keyLen = DbfInfo.IDX[thisIDX].KeyLen;

            if (recSkip != 0)
            {
                if (DbfInfo.IDX[thisIDX].RecordStatus.NodeMap.Count == 0)
                {
                    // got an error
                }
                else
                {
                    try
                    {
                        IDXNode node = await IDXGetNode(DbfInfo.IDX[thisIDX].RecordStatus.NodeMap[^1].Position, thisIDX);
                        int nodeRec = DbfInfo.IDX[thisIDX].RecordStatus.NodeMap[^1].NodeRecord;
                        int getRec = -1;
                        while (true)
                        {
                            if (recSkip < 0)
                            {
                                // moving backwards
                                if (nodeRec + recSkip < 0)
                                {
                                    // Get the previous node
                                    recSkip = recSkip - nodeRec - 1;

                                    if (node.LeftPtr > 0)
                                    {
                                        node = await IDXGetNode(node.LeftPtr, thisIDX);
                                        nodeRec = node.Keys - 1;
                                    }
                                    else
                                    {
                                        getRec = 0;
                                        break;
                                    }
                                }
                                else
                                {
                                    getRec = nodeRec + recSkip;
                                    break;
                                }
                            }
                            else
                            {
                                // Moving forward
                                if (nodeRec + recSkip >= node.Keys)
                                {
                                    // Get the next node
                                    recSkip -= (node.Keys - nodeRec - 1);

                                    if (node.RightPtr > 0)
                                    {
                                        node = await IDXGetNode(node.RightPtr, thisIDX);
                                        nodeRec = 0;
                                    }
                                    else
                                    {
                                        getRec = node.Keys - 1;
                                        break;
                                    }
                                }
                                else
                                {
                                    getRec = nodeRec + recSkip;
                                    break;
                                }
                            }
                        }

                        // Grab the record pointed to by getRec for this node
                        int keyPos = 12 + getRec * (keyLen + 4);
                        byte[] recBytes = new byte[4];
                        Array.Copy(node.Buffer, keyPos, DbfInfo.IDX[thisIDX].RecordStatus.Key, 0, keyLen);
                        Array.Copy(node.Buffer, keyPos + +keyLen, recBytes, 0, 4);
                        int recNo = App.utl.RevBin2Int(recBytes);
                        DbfInfo.IDX[thisIDX].RecordStatus.Record = recNo;
                        //DbfInfo.IDX[thisIDX].DBFRecord = recNo;
                        //DbfInfo.IDX[thisIDX].NodeRecord = getRec;
                        //DbfInfo.IDX[thisIDX].Position = node.Position;
                        DbfInfo.IDX[thisIDX].RecordStatus.Command = 1;
                        //DbfInfo.IDX[thisIDX].recordStatus.NodeRecord = getRec;
                        DbfInfo.IDX[thisIDX].RecordStatus.NodeMap[^1].Position = node.Position;
                        DbfInfo.IDX[thisIDX].RecordStatus.NodeMap[^1].NodeRecord = getRec;
                    }
                    catch (Exception ex)
                    {
                        AppErrorHandling.SetError(8019, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        DbfInfo.IDX[thisIDX].RecordStatus.Command = -99;
                    }
                }
            }
            else
                DbfInfo.IDX[thisIDX].RecordStatus.Command = 1; // no move = found it!

            return DbfInfo.IDX[thisIDX].RecordStatus;

        }



        /*===================================================================================*
         * TODO - CDX
         * NEED - information on compression logic and more info on the struture
         * 
         * 
         * Byte offset Description
         *       00 – 03 Pointer to root node
         *       04 – 07 Pointer to free node list ( -1 if not present)
         *       08 – 11 Reserved for internal use
         *       12 – 13 Length of key
         *       14 Index options (any of the following numeric values or their sums):
         *       1–a unique index
         *       8–index has FOR clause
         *       32 –compact index format
         *       64 –compound index header
         *       15 Index signature
         *       16 – 19 Reserved for internal use
         *       20 – 23 Reserved for internal use
         *       24 – 27 Reserved for internal use
         *       28 – 31 Reserved for internal use
         *       32 – 35 Reserved for internal use
         *       36 – 501 Reserved for internal use
         *       502 – 503  Ascending or descending:
         *       0 = ascending
         *       1 = descending
         *       504 – 505 Reserved for internal use
         *       506 – 507 FOR expression pool length1
         *       508 – 509 Reserved for internal use
         *       510 – 511 Key expression pool length1
         *       512 – 1023 Key expression pool (uncompiled)
         *
         *
         *       1 This information tracks the space used in the key expression pool.
         *
         *       Compact Index Interior Node Record
         *
         *       Byte offset Description
         *       00 – 01  Node attributes (any of the following numeric values or their sums):
         *       a.0 – index node
         *       b.1 – root node
         *       c.2 – leaf node
         *       02 – 03 Number of keys present (0, 1 or many)
         *       04 – 07 Pointer to node directly to left of current node (on same level, -1 if not present)
         *       08 – 11 Pointer to node directly to right of current node (on same level; -1 if not present)
         *       12 – 511 Up to 500 characters containing the key value for the length of the key with a 
         *       four-byte hexadecimal number (stored in normal left-to-right format):
         *       This node always contains the index key, record
         *       number and intra-index pointer.2
         *
         *       The key/four-byte hexadecimal number combinations will occur the number of times indicated in bytes 02 – 03.
         *
         *
         *
         *       Compact Index Exterior Node Record
         *
         *       00 – 01  Node attributes (any of the following numeric values or their sums):
         *       0 – index node
         *       1 – root node
         *       2 – leaf node
         *       02 – 03 Number of keys present (0, 1 or many)
         *       04 – 07  Pointer to the node directly to the left of current node (on same level; -1 if not present)
         *       08 – 11  Pointer to the node directly to right of the current node (on same level; -1 if not present)
         *       12 – 13 Available free space in node
         *       14 – 17 Record number mask
         *       18 Duplicate byte count mask
         *       19 Trailing byte count mask
         *       20 Number of bits used for record number
         *       21 Number of bits used for duplicate count
         *       22 Number of bits used for trail count
         *       23  Number of bytes holding record number, duplicate count and trailing count
         *       24 – 511 Index keys and information2 
         *===================================================================================*/
        public async Task<bool> CDXOpen(string FullFileName)
        {
            bool llSuccess = true;
            AppErrorHandling.ClearErrors();

            try
            {
                throw new Exception("1999||CDX not implemented");
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(8007, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return llSuccess;
        }

        /*-----------------------------------------------------------------------------------*
         *-----------------------------------------------------------------------------------*/
        public async Task<bool> CDXCreate()
        {
            bool llSuccess = true;
            AppErrorHandling.ClearErrors();

            try
            {
                throw new Exception("1999||CDX not implemented");
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(8020, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return llSuccess;
        }

        /*-----------------------------------------------------------------------------------*
         *-----------------------------------------------------------------------------------*/
        public async Task<bool> CDXWrite(DataTable dt)
        {
            bool llSuccess;
            AppErrorHandling.ClearErrors();

            try
            {
                throw new Exception("1999||CDX not implemented");
            }
            catch (Exception ex)
            {
                llSuccess = false;
                AppErrorHandling.SetError(8020, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return llSuccess;
        }



        /*===================================================================================*
         *===================================================================================*
         * Memo File structure
         * 
         * Header
         *      Bytes    Description
         *      00 – 03  Location of next free block - typically end of file
         *      04 – 05  Unused
         *      06 – 07  Block size (bytes per block)
         *      08 - 15  0x00 filled
         *      16 - 51  TODO - GUID tag from DBF
         *      52 – 511 Unused
         * 
         * Memo Block
         *      Bytes   Description
         *      00 – 03 Block signature: 0=binary, 1=text
         *      04 – 07 Length of memo (in bytes)
         *      08 – n  Memo Data
         *      
         * All values, except Memo Data, are integers stored with the most significant byte first.
         *      Examples:   0x00 0x00 0x01 0x01 = 1*256 + 1 = 257
         *                  0x40 0x31           = 4*16*256 + 3*16+1 = 16,433
         *
         *-----------------------------------------------------------------------------------*/
        /// <summary>
        /// Read a memo file return a bool for success and send OUT the text
        /// </summary>
        /// <param name="block"></param>
        /// <param name="memoText"></param>
        /// <returns></returns>
        public async Task<string> FPTReadText(int block)
        {
            string memoText = string.Empty;

            try
            {
                byte[] outBuffer = await FPTRead(block);
                memoText = System.Text.Encoding.UTF8.GetString(outBuffer);

                // What did we get?
                if (memoText.Length > 50)
                    AppIO.DebugLog(string.Format("Reading block {0} - return length={1} - {2}...", block, memoText.Length, memoText[..50]));
                else
                    AppIO.DebugLog(string.Format("Reading block {0} - {1}", block, memoText));
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(8026, "Failed to convert memo to text - " + ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return memoText;
        }


        /*-----------------------------------------------------------------------------------*
         * Get data from the memo file starting at the specified block
         *-----------------------------------------------------------------------------------*/
        /// <summary>
        /// Read a block from the memo file, return a bool for success and send OUT a byte array
        /// </summary>
        /// <param name="block"></param>
        /// <param name="outBuffer"></param>
        /// <returns></returns>
        public async Task<byte[]> FPTRead(int block)
        {
            byte[] outBuffer = new byte[1];

            try
            {
                DbfInfo.Memo.BlockType = 0;
                DbfInfo.Memo.TextLen = 0;

                long blockPos = DbfInfo.Memo.BlockSize * block;

                if (blockPos > 511 && blockPos >= DbfInfo.Memo.NextFree * DbfInfo.Memo.BlockSize)
                {
                    AppErrorHandling.SetError(8026, "Invalid location specified for memo field", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                }
                else
                {
                    // Grab the memo field
                    byte[] memoBuffer = new byte[4];
                    DbfInfo.MemoStream!.Position = blockPos;
                    DbfInfo.MemoStream!.ReadExactly(memoBuffer);
                    DbfInfo.Memo.BlockType = (int)App.utl.RevBin2Long(memoBuffer);

                    memoBuffer = new byte[4];
                    DbfInfo.MemoStream!.ReadExactly(memoBuffer);
                    DbfInfo.Memo.TextLen = (int)App.utl.RevBin2Long(memoBuffer);

                    outBuffer = new byte[DbfInfo.Memo.TextLen];
                    DbfInfo.MemoStream.ReadExactly(outBuffer);
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(8026, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return outBuffer;
        }

        /*-----------------------------------------------------------------------------------*
         * We don't update old memo blocks as they will be removed when memo is packed
         * so we'll always append to the end of the file.
         * 
         * Return the integer byte array representing the location of the newly written 
         * block.  If the byte array is -1 (all 255) then it failed to write.
         *-----------------------------------------------------------------------------------*/
        /// <summary>
        /// Write text to the end of the memo file
        /// </summary>
        /// <param name="memoText"></param>
        /// <returns></returns>
        private async Task<byte[]> FPTWriteText(string memoText)
        {
            byte[] negOne = [255, 255, 255, 255];
            byte[] blockNumber = negOne;

            try
            {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(memoText);
                blockNumber = await FPTWrite(buffer, false);
            }
            catch (Exception ex)
            {
                blockNumber = negOne;
                AppErrorHandling.SetError(8025, "Failed to convert memo to text - " + ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return blockNumber;
        }



        /*===================================================================================*
         *===================================================================================*
         * Write the buffer to a memo file.  Typically it's appended, but not always
         * as it's possible to write empty blocks to the end of the file.
         *-----------------------------------------------------------------------------------*/
        /// <summary>
        /// Write a byte array to the end of the memo file
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="isBinary"></param>
        /// <returns></returns>
        private async Task<byte[]> FPTWrite(byte[] buffer, bool isBinary)
        {
            byte[] blockNumber = [255, 255, 255, 255];
            byte[] nBytes;

            try
            {
                // write the block to the end of the memo file
                if (DbfInfo.MemoStream is not null)
                {
                    // Calculate the number of blocks to write
                    int blocksWritten = (buffer.Length + 8) / DbfInfo.Memo.BlockSize;
                    if (buffer.Length > blocksWritten * DbfInfo.Memo.BlockSize) blocksWritten++;

                    // Create buffer that is the full size of the area to write
                    byte[] buffer2Write = new byte[blocksWritten * DbfInfo.Memo.BlockSize];

                    // Fill the write buffer header
                    buffer2Write[3] = (byte)(isBinary ? 0 : 1);     // Data type (0=binary, 1=text)
                    nBytes = App.utl.Long2Bin(buffer.Length);
                    Array.Copy(nBytes, 4, buffer2Write, 8, 4);      // Data length in bytes

                    // Add what's being written
                    Array.Copy(buffer, 0, buffer2Write, 12, buffer.Length);


                    // Calculate where to write and blocks written
                    long writePosition = DbfInfo.Memo.NextFree * DbfInfo.Memo.BlockSize;

                    if (writePosition < DbfInfo.MemoStream.Length)
                    {
                        // ERROR - not at end of file
                        blockNumber = [255, 255, 255, 255];
                        AppErrorHandling.SetError(8025, "Write position failure", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    }
                    else
                    {
                        // Write the data to the end of the memo file
                        DbfInfo.MemoStream.Position = writePosition;
                        DbfInfo.MemoStream.Write(buffer2Write);
                        DbfInfo.MemoStream.Flush();

                        // Remember to what block it was written
                        blockNumber = App.utl.RevInt2Bin(DbfInfo.Memo.NextFree);

                        // Calculate the next free block to the memo header
                        DbfInfo.Memo.NextFree = (writePosition + blocksWritten * DbfInfo.Memo.BlockSize) / DbfInfo.Memo.BlockSize;

                        // Write the next free block to the header
                        nBytes = App.utl.RevInt2Bin(DbfInfo.Memo.NextFree);
                        byte[] nextFree = [0, 0, 0, 0];
                        Array.Copy(nextFree, 4, nBytes, 0, 4);

                        DbfInfo.MemoStream.Position = 0;
                        DbfInfo.MemoStream.Write(nextFree);
                        DbfInfo.MemoStream.Flush();
                    }
                }
                else
                    throw new Exception("Memo file is not open");
            }
            catch (Exception ex)
            {
                blockNumber = [255, 255, 255, 255];
                AppErrorHandling.SetError(8025, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return blockNumber;
        }

        /*===================================================================================*
         * TODO - JSON File support
         * JSON Fields are fixed length from 32 to 8,192 bytes in 32 byte blocks
         *===================================================================================*/


        /*===================================================================================*
         * Utility routines
         *===================================================================================*/
        /// <summary>
        /// Read in a record and quickly grab the value for the field
        /// </summary>
        /// <param name="row"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public async Task<List<JAXObjects.Token>> FastFieldValue(int row, List<JAXTables.FieldInfo> fields)
        {
            List<JAXObjects.Token> results = [];

            try
            {
                byte[] buffer = GetRecord(row, false);

                foreach (JAXTables.FieldInfo field in fields)
                {
                    JAXObjects.Token v = await GetFieldValueFromRecordBuffer(buffer, field);
                    results.Add(v);
                }
            }
            catch (Exception ex)
            {
                results = [];
                AppErrorHandling.SetError(8804, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return results;
        }

        public void Dispose()
        {
        }
    }
}

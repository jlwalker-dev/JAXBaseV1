CLOSE ALL
CLEAR ALL
SET CONSOLE on
SET CONSOLE to inactive
CLEAR
? "Running TEST1"

CREATE TABLE TestJAX1 (Name c(30),balance n(8,2), visits i, age n(2,0))
USE

? "Creating table"
USE testjax1
INSERT INTO TestJax1 (Name,balance,visits,age) VALUES ("Jon",18192.17,10,64)
INSERT INTO TestJax1 (Name,balance,visits,age) VALUES ("sue",38483.25,38,75)
INSERT INTO TestJax1 (Name,balance,visits,age) VALUES ("michael",5000,5,46)

? alias(),"created!"

* Do speed trial #1
? "Starting speed test"
Start=SECONDS()
AVERAGE balance,visits,age ALL TO bal,vis,aj
CALCULATE AVG(age) ALL FOR age>60 TO ageism
SUM ALL age TO totalyears
USE

SpeedTest1=SECONDS()-start
? "Lap=",SpeedTest1

* Checking out IF
IF SECONDS()<84600/2
	ZEND="GOOD MORNING"
ELSE 
	ZEND = "GOOD AFTERNOON"
ENDIF 

* Create some output
=STRTOFILE("Testing - "+TRANSFORM(DATETIME())+CHR(13)+CHR(10),"c:\jax\tests\output2.txt",0)
FOR i=1 TO 30
	j=i
	=STRTOFILE("Loop "+TRANSFORM(i)+CHR(13)+CHR(10),"c:\jax\tests\output2.txt",1)
ENDFOR

* Quick while test
DO while i>5
	i=i-1
enddo

LIST MEMORY to FILE c:\jax\tests\Output1.txt
? "Total Speed=",SECONDS()-start

WAIT WINDOW "Done!" TIMEOUT 5

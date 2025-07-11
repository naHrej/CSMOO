grammar Command;

// Parser rules
command : loginCommand | createPlayerCommand | gameCommand | scriptCommand ;

loginCommand : LOGIN STRING STRING ;
createPlayerCommand : CREATE PLAYER STRING STRING ;
gameCommand : IDENTIFIER arguments? ;
scriptCommand : SCRIPT csharpCode ;

arguments : argument (',' argument)* ;
argument : STRING | NUMBER | IDENTIFIER ;

csharpCode : '{' .*? '}' ;

// Lexer rules
LOGIN : 'login' ;
CREATE : 'create' ;
PLAYER : 'player' ;
SCRIPT : 'script' ;

IDENTIFIER : [a-zA-Z_][a-zA-Z0-9_]* ;
STRING : '"' (~["\r\n])* '"' | '\'' (~['\r\n])* '\'' ;
NUMBER : [0-9]+ ('.' [0-9]+)? ;

WS : [ \t\r\n]+ -> skip ;

COPY	START	0	EXTDEF	BUFFER, BUFEND, LENGHT	EXTREF	RDREC, WRREC0000	FIRST	STL	RETADR			172027
0003	CLOOP	+JSUB	RDREC			4B100000
0007		LDA	LENGHT			032023
000A		COMP	#0			290000
000D		JEQ	ENDFIL			332007
0010		+JSUB	WRREC			4B100000
0014		J	CLOOP			3F2FEC
0017	ENDFIL	LDA	#22			010016
001A		STA	BUFFER			0F2013
001D		LDA	#3			010003
0020		STA	LENGHT			0F200A
0023		+JSUB	WRREC			4B100000
0027		J	@RETADR			3E2000
002A	RETADR	RESW	1
002D	LENGHT	RESW	1
0030	BUFFER	RESB	10
003A	BUFEND	EQU	*
003B	MAXLEN	EQU	BUFEND-BUFFER
..	SUB-ROTINA PARA GUARDAR REGISTRO NO BUFFER.RDREC	CSECT	EXTREF	BUFFER, LENGHT, BUFEND0000		CLEAR	X			B410
0002		CLEAR	A			B400
0004		LDS	#48			6D0030
0007		LDT	MAXLEN			77201F
000A	RLOOP	TD	INPUT			E3201B
000D		JEQ	RLOOP			332FFA
0010		RD	INPUT			DB2015
0013		COMPR	A,S			A004
0015		JEQ	EXIT			332009
0018		+STCH	BUFFER,X			57900000
001C		TIXR	T			B850
001E		JLT	RLOOP			3B2FE9
0021	EXIT	+STX	LENGHT			13100000
0025		RSUB			4F0000
0028	INPUT	BYTE	X'F1'		F1
0029	MAXLEN	WORD	BUFEND-BUFFER		000000
..	SUB-ROTINA PARA TRANSFERIR REGISTRO DO BUFFER.WRREC	CSECT	EXTREF	LENGHT, BUFFER0000		CLEAR	X			B410
0002		+LDT	LENGHT			77100000
0006	WLOOP	TD	OUTPUT			E32012
0009		JEQ	WLOOP			332FFA
000C		+LDCH	BUFFER,X			53900000
0010		WD	OUTPUT			DF2008
0013		TIXR	T			B850
0015		JLT	WLOOP			3B2FEE
0018		RSUB			4F0000
001B	OUTPUT	BYTE	X'05'		05
	END	FIRST

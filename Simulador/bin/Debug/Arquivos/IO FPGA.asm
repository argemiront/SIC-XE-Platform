FPGA	START	0
RLOOP	TD	CHAVE
	JEQ	RLOOP
	RD	CHAVE
	WD	CHAVE
	J	RLOOP
	SVC	4
ZERO	WORD	0
CHAVE	BYTE	X'01'
	END	0
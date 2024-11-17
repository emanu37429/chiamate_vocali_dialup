# chiamate_vocali_dialup
Usando un modem dial-up con supporto alla voce (verificabile connettendosi alla seriale e dando il comando AT+FCLASS=? o AT#CLS=? da terminale) è possibile effettuare chiamate vocali dal proprio PC. In questa repo lascio un programmino di esempio scritto in C#.

Per effettuare una chiamata col modem che ho usato io (chipset CX93010) si fa come segue:
1) Comando "AT" per verificare di poter comunicare col modem > Risposta "OK"
2) Comando "AT+FCLASS=8" per impostare la modalità voce (8) > Risposta "OK"
3) Comando "AT+VSM=131,8000" per impostare il codec G711 uLaw. È possibile anche impostare diversi codec (per verificare quelli supportati, dare "AT+VSM=?") posto di modificare poi il programma per dare l'audio nel formato appropriato > Risposta "OK"
4) Comando "ATDTxxxxxxxxxx" dove le x sono il numero di telefono per effettuare la chiamata > Risposta "VCON" (o simile) quando il ricevente risponde alla chiamata.
5) Quando si riceve "VCON" si può far partire la comunicazione full duplex dando "AT+VTR". A questo punto si comincerà a ricevere direttamente da seriale l'audio nel formato selezionato e tutto quello che verrà scritto sulla seriale verrà interpretato dal modem come audio nel formato selezionato. Nel flusso di dati ci potrebbero essere dati di controllo, preceduti dal carattere Data Link Escape (0x10). Per ascoltare l'audio essi andranno eliminati dal flusso di byte. Se invece capita che qualche byte che occorre naturalmente nel flusso audio abbia valore 0x10, esso sarà ripetuto due volte e noi dovremmo leggerlo una volta sola. Viceversa, quando inviamo, se nel flusso abbiamo dei 0x10 dovremmo duplicarli.

Se usate modem con chipset diversi dal CX93010, suggerisco di verificare la documentazione del vostro chipset perché i comandi potrebbero variare.

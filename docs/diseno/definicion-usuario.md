# Definición de la app (palabras del usuario, 2026-10-22)

> Copia literal de lo que pidió el usuario. Fuente de verdad para el GDD.

Vale, ahora vamos a definir la app, y quiero que entre el planificador y el diseñador decidan como avanzar en ella, si es necesario deberian poder hablar con el arquitecto para definir los sprints.

La herramienta es una herramienta para movil, por ahora para android, que hara lo siguiente.

Vamos a empezar por una pantalla de inicio con dos botones, Cargar historias y leer historias.

En cargar historias veremos una pantalla con un boton para cargar, esto abrira el buscador de nuestro movil para encontrar alchivos .txt .

Al cargarlo, el archivo tendra lo siguiente.

Un texto plano, pero de vez en cuando se encontrara pedazos de texto envueltos en {} con el siguiente formato

{Kanji: ; Reading: ;Meaning: ; Sentence. ; SentenceMeaning:;} Entre los dos puntoss y el punto y coma estaria el valor de cada variable.

Esto se deberia guardar en un archivo o Json o lo que el arquitecto decida que es mejor y mas facil de manejar a futuo.

Cuando vayamos a lista de historias habra una lista con cada uno de estos archivos, o secciones o como el arquitecto crea correcto guardarlos.

El tema es que saldra una lista en la que si clikamos sale un pop up diciendo si queremos leerlo o eliminarlo. En caso de eliminarlo deberia dar un aviso antes de borrarlo para asegurarse de que no borramos lo que no queremos.

El tema es que cuando abrimos la historia saldra la historia que habia en el archivo de manera scrolleable. Pero donde estaban las llaves, se vera lo siguiente.

La palabra en la variable Kanji, en azul y clickable.

Si clickamos veremos un popup donde vendra.

El kanji en grande, y la frase de ejemplo abajo.

Habra dos botones, uno cerrara el popup para seguir leyendo. El otro cambiara el contendio del popup para que se vean tambien la lectura, la traduccion y la traduccion de la frase.

Aqui habra un boton de cerrar pop up y otro de añadir a kanjis complicados.

Cuando se cierre la historia, saldra a su lado una estadistica de cuantos kanjis tuvimos que revisar con lectura y todo para saber cuales el porcentaje de acierto que tenemos.

Los kanjis dificiles seran añadidos a una lista, en la que estaran y al clickarlos saldra un popup como el anterior. Solo que esta vez habra tambien un boton de eliminar de lista si ya lo sabemos.

Aunque inicialmente dijimos que habia solo dos botones al entrar, tambien vamos a poner el de kanjis complicados y el de exit para cerrar la app

Primero diseña las fases y los sprints, luego tras mi confirmacion iremos avanzando

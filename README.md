# ospConverter
ist ein Programm zur Umsetzung von GeoTiff-Dateien in Koordinatenlisten. Die Konvertierung erfolgt mit der [gdal-Bibliothek](https://gdal.org). Die Bibliothek steht unter einer [MIT-ähnlichen Lizenz](https://gdal.org/license.html)
## Haftungsausschluss
Dieses Programm ist das erste, das ich hier hochlade. Außerdem ist "ich" kein echter Programmierer, sondern nur einer der das als Hobby macht. Entsprechend übernehme ich (natürlich) keine Garantie, dass alles funktioniert. Beim mir konvertiert es die Dateien - und das ist für mich das wichtigste. Wenn es sonst noch jemandem nutzt: umso besser!
## Was das Programm macht
Im Rahmen der OpenData-Umstellung beim Vermessungsamt wird das Digitale Geländemodell nicht mehr als Koordinatenliste in verschiedenen Rasterweiten abgegeben, sondern nur noch als GeoTiff-Datei mit 1 m Rasterweite. GeoTiff-Dateien können aber nicht in Stanet eingelesen werden. Der ospConverter setzt mit der gdal-Bibliothek die Dateien um und speichert sie als txt-Datei. Genau genommen erfolgt mit gdal-transform eine Batch-Konvertierung.
Außerdem kann die Rasterweite angepasst werden.

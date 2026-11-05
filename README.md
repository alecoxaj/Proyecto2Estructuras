# Sistema de Gestión de Transporte Urbano

UrbanRoute es una aplicación de escritorio desarrollada en **C# con WPF (.NET 10)** enfocada en la gestión, búsqueda y optimización de rutas de transporte público. 

Este proyecto destaca por su riguroso enfoque algorítmico: **el backend fue construido desde cero implementando estructuras de datos puras** sin depender de las colecciones nativas de C# (como `List<T>` o `Dictionary<K,V>`).

## Características Principales

1. **Gestión de Paradas (Tabla Hash):** 
   * Inserción, búsqueda y eliminación de paradas en tiempo constante $O(1)$.
   * Manejo de colisiones implementado manualmente.
2. **Cálculo de Rutas Óptimas (Dijkstra):** 
   * Evaluación de caminos más rápidos (optimización por tiempo).
   * Evaluación de caminos más económicos (optimización por costo).
   * Despliegue del itinerario paso a paso.
3. **Autocompletado Rápido (Trie):** 
   * Búsqueda en tiempo real de nombres de paradas por prefijo, ideal para barras de búsqueda.
4. **Análisis de Red (Grafos & Union-Find):** 
   * Representación de la ciudad mediante una Lista de Adyacencia.
   * Recorridos de exploración en Anchura (BFS) y Profundidad (DFS).
   * Verificación de conectividad entre zonas aisladas utilizando *Union-Find* (Disjoint Set).
5. **Persistencia de Datos:** 
   * Lectura automática al arrancar desde archivos `.csv` (`paradas.csv` y `rutas.csv`).

## Arquitectura del Proyecto

El código está dividido bajo el principio de responsabilidad única en dos proyectos principales:

* **`Proyecto2Estructuras` (Backend / Class Library):** Contiene toda la lógica matemática.
  * `/Estructuras`: `Grafo`, `TablaHash`, `TrieParadas`, `ConectividadParadas` (Union-Find).
  * `/Modelos`: Clases puras de objetos (`Parada`, `Ruta`).
  * `/Datos`: Lectores y parseadores de CSV.
* **`UrbanRoute.UI` (Frontend / WPF):** Interfaz gráfica "Dark Mode" de la aplicación.
  * Implementa diseño responsivo y generadores dinámicos de vistas.
  * Mantiene el estado e invoca los servicios del backend.

## Cómo ejecutar el proyecto

### Requisitos
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download) o superior.
* Visual Studio 2022 o Visual Studio Code con la extensión de C# instalada.

### Instrucciones por Terminal
1. Clona o descarga este repositorio.
2. Abre una terminal y navega hasta la carpeta de la interfaz gráfica:
   ```bash
   cd UrbanRoute.UI
   ```
3. Restaura las dependencias (si es la primera vez):
   ```bash
   dotnet restore
   ```
4. Ejecuta la aplicación:
   ```bash
   dotnet run
   ```

## Notas de Desarrollo (Merge y Limpieza)
Si actualizas el código a través de Git (pull/merge) y el compilador lanza errores de caché (`CS0246`), asegúrate de limpiar los rastros de compilaciones anteriores:
1. Elimina manualmente las carpetas `bin` y `obj`.
2. Ejecuta `dotnet clean` en tu terminal antes de volver a compilar.
3. La interfaz principal se encuentra en MainWindow.xaml.cs, mientras que form1 existe por precaucion y pruebas rápidas


---
*Desarrollado como proyecto de Estructuras de Datos.*



// https://www.andrew.cmu.edu/course/15-121/lectures/Binary%20Heaps/code/Heap.java

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PriorityQueue {

   public int size;            // Number of elements in heap
   public TerrainGenerator.Pixel[] heap;     // The heap array

   public PriorityQueue(int capacity= 16384)
   {
      size = 0;
      heap = new TerrainGenerator.Pixel[capacity];
   }

   
   public PriorityQueue(TerrainGenerator.Pixel[] array, int capacity = 16384)
   {
      size = array.Length;
      heap = new TerrainGenerator.Pixel[capacity];

      array.CopyTo(heap, 1);

      BuildHeap();
   }
 
   private void BuildHeap()
   {
      for (int k = size/2; k > 0; k--)
      {
         PercolatingDown(k);
      }
   }
   private void PercolatingDown(int k)
   {
      TerrainGenerator.Pixel tmp = heap[k];
      int child;

      for(; 2*k <= size; k = child)
      {
         child = 2*k;

         if(child != size &&
            heap[child].dist > heap[child + 1].dist) child++;

         if(tmp.dist > heap[child].dist)  heap[k] = heap[child];
         else
                break;
      }
      heap[k] = tmp;
   }
   
   
   public TerrainGenerator.Pixel PopMin()
   {
      TerrainGenerator.Pixel min = heap[1];
      heap[1] = heap[size--];
      PercolatingDown(1);
      return min;
	}

   public void Insert( TerrainGenerator.Pixel x)
   {
      if(size == heap.Length - 1) DoubleSize();

      //Insert a new item to the end of the array
      int pos = ++size;

      //Percolate up
      for(; pos > 1 && x.dist < heap[pos/2].dist; pos = pos/2 )
         heap[pos] = heap[pos/2];

      heap[pos] = x;
   }
   private void DoubleSize()
   {
      TerrainGenerator.Pixel [] old = heap;
      heap = new TerrainGenerator.Pixel[heap.Length * 2];
      old.CopyTo(heap, 1);
   }

   public String toString()
   {
      String result = "";
      for(int k = 1; k <= size; k++) result += heap[k]+" ";
      return result;
   }
}
using System;
using TMPro;
using UnityEngine;

public class Cell : MonoBehaviour
{
    public int row, col;
    public Caro manager; // Changed type to GameManager

    public void OnMauseDown()
    {
        manager.HandlePlayerMove(row, col); // Assuming GameManager has this method
    }

    public void SetSymbol(string symbol)
    {
        GetComponentInChildren<TextMeshProUGUI>().text = symbol;
    }
}

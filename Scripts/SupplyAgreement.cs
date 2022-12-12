using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SupplyAgreement : MonoBehaviour
{
    [SerializeField] private NanoGrid consumerNanoGrid;
    [SerializeField] private NanoGrid supplierNanoGrid;
    [SerializeField] private float tariffIoenFuel;
    [SerializeField] private float transactionEnergyLimit;
    [SerializeField] private float creditLimit;

    public void SetConsumerNanoGrid(NanoGrid consumerNanoGrid)
    {
        this.consumerNanoGrid = consumerNanoGrid;
    }

    public NanoGrid GetConsumerNanoGrid()
    {
        return consumerNanoGrid;
    }

    public void SetSupplierNanoGrid(NanoGrid supplierNanoGrid)
    {
        this.supplierNanoGrid = supplierNanoGrid;
    }

    public NanoGrid GetSupplierNanoGrid()
    {
        return supplierNanoGrid;
    }

    public void SetTariffIoenFuel(float tariffIoenFuel)
    {
        this.tariffIoenFuel = tariffIoenFuel;
    }

    public float GetTariffIoenFuel()
    {
        return tariffIoenFuel;
    }

    public void SetTransactionEnergyLimit(float transactionEnergyLimit)
    {
        this.transactionEnergyLimit = transactionEnergyLimit;
    }

    public float GetTransactionEnergyLimit()
    {
        return transactionEnergyLimit;
    }

    public void SetCreditLimit(float creditLimit)
    {
        this.creditLimit = creditLimit;
    }

    public float GetCreditLimit()
    {
        return creditLimit;
    }
}
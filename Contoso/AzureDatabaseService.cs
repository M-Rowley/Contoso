using Microsoft.WindowsAzure.MobileServices;
using Contoso.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Contoso
{
    public class AzureDatabaseService
    {
        //Initialise instance and client
        public static AzureDatabaseService instance;
        public MobileServiceClient client;
        public IMobileServiceTable<ContosoBankAccounts> bankAccountTable;

        //explicit constructor
        AzureDatabaseService()
        {
            this.client = new MobileServiceClient("http://MrowContosoMobile.azurewebsites.net");
            this.bankAccountTable = this.client.GetTable<ContosoBankAccounts>();
        }

        //POST (C)
        public async Task addAccount(ContosoBankAccounts bankAccount)
        {
            await this.bankAccountTable.InsertAsync(bankAccount);
        }

        //GET (R)
        public async Task<List<ContosoBankAccounts>> getAccounts()
        {
            return await this.bankAccountTable.ToListAsync();
        }

        //PUT (U)
        public async Task updateAccount(ContosoBankAccounts bankAccount)
        {
            await this.bankAccountTable.UpdateAsync(bankAccount);
        }

        //DELETE (D)
        public async Task deleteAccount(ContosoBankAccounts bankAccount)
        {
            await this.bankAccountTable.DeleteAsync(bankAccount);
        }
    }
}
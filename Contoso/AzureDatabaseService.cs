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
        private static AzureDatabaseService instance;
        private MobileServiceClient client;
        private IMobileServiceTable<ContosoBankAccounts> bankAccountTable;

        //explicit constructor
        private AzureDatabaseService()
        {
            this.client = new MobileServiceClient("http://MrowContosoMobile.azurewebsites.net");
            this.bankAccountTable = this.client.GetTable<ContosoBankAccounts>();
        }

        //publicly accessible client get method
        public MobileServiceClient AzureClient
        {
            get { return client; }
        }

        //publicly accessible instance get method
        public static AzureDatabaseService AzureDatabaseServiceInstance
        {
            get
            {
                //creates instance of static variable instance upon call if doesn't already exist 
                if (instance == null)
                {
                    instance = new AzureDatabaseService();
                }
                return instance;
            }
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
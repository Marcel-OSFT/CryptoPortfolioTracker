using System;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;

namespace CryptoPortfolioTracker.Services
{
    public class MutationMockService
    {
        static Guid txGuid1 = Guid.NewGuid();
        static Guid txGuid2 = Guid.NewGuid();

        public readonly Mutation MockMutation1_in = new()
        {
            Type = TransactionKind.Deposit,
            Qty = 0.05,
            Direction = MutationDirection.Out,
            //AcountId = 1,
            //AssetId = 1,

        };
        public readonly Mutation MockMutation1_out = new()
        {
            Type = TransactionKind.Deposit,
            Qty = 0.05,
            Direction = MutationDirection.In,
            //AcountId = 2,
            //Asset = 1,

        }; public readonly Mutation MockMutation2_in = new()
        {
            Type = TransactionKind.Deposit,
            Qty = 0.1,
            Direction = MutationDirection.Out,
            //AcountId = 2,
            //AssetId = 2,

        };
        public readonly Mutation MockMutation2_out = new()
        {
            Type = TransactionKind.Deposit,
            Qty = 0.1,
            Direction = MutationDirection.In,
            //AcountId = 1,
            //AssetId = 2,

        };
    }
}


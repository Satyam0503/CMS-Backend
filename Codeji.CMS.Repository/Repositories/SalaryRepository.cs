using MongoDB.Driver;
using Codeji.CMS.DTO.Salary;
using Codeji.CMS.Repository.Interfaces;

namespace Codeji.CMS.Repository.Repositories
{
    public class SalaryRepository : ISalaryRepository
    {
        private readonly IMongoCollection<SalaryModel> _salaryCollection;

        public SalaryRepository(MongoDbContext context)
        {
            _salaryCollection = context.GetCollection<SalaryModel>();
        }

        public async Task<SalaryModel?> GetActiveSalaryAsync(Guid userId)
        {
            var filter = Builders<SalaryModel>.Filter.Eq(s => s.UserId, userId) &
                         Builders<SalaryModel>.Filter.Eq(s => s.Status, true);
            return await _salaryCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<List<SalaryModel>> GetActiveSalariesAsync(List<Guid> userIds)
        {
            var filter = Builders<SalaryModel>.Filter.In(s => s.UserId, userIds) &
                         Builders<SalaryModel>.Filter.Eq(s => s.Status, true);
            return await _salaryCollection.Find(filter).ToListAsync();
        }

        public async Task<List<SalaryModel>> GetSalaryHistoryAsync(Guid userId)
        {
            var filter = Builders<SalaryModel>.Filter.Eq(s => s.UserId, userId);
            return await _salaryCollection.Find(filter).SortByDescending(s => s.EffectiveFrom).ToListAsync();
        }

        public async Task AddAsync(SalaryModel salary)
        {
            await _salaryCollection.InsertOneAsync(salary);
        }

        public async Task UpdateAsync(SalaryModel salary)
        {
            var filter = Builders<SalaryModel>.Filter.Eq(s => s.SalaryId, salary.SalaryId);
            await _salaryCollection.ReplaceOneAsync(filter, salary);
        }
    }
}

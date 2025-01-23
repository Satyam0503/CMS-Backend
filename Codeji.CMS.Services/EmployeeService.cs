using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Interface;

namespace Codeji.CMS.Services
{
    public class EmployeeService : IEmployeeService
    {
        readonly IMongoDbRepository<EducationDetails> _educationDetailsRepo;
        readonly IMongoDbRepository<CertificationDetails> _certificationDetailsRepo;
        readonly IMongoDbRepository<UserSummary> _userSummaryRepo;
        readonly IMapper _mapper;

        public EmployeeService(IMongoDbRepository<EducationDetails> educationDetailsRepo, IMapper mapper, IMongoDbRepository<CertificationDetails> certificationDetailsRepo, IMongoDbRepository<UserSummary> userSummary)
        {
            _educationDetailsRepo = educationDetailsRepo;
            _mapper = mapper;
            _certificationDetailsRepo = certificationDetailsRepo;
            _userSummaryRepo = userSummary;
        }

        public async Task<Result<EmployeeSummaryRequestModel>> AddEmployeeSummary(EmployeeSummaryRequestModel userSummary, string userId, string companyId)
        {
            UserSummary summary = new UserSummary()
            {
                CompanyId = companyId,
                UserId = userId,
                EmployeeSummary = userSummary.EmployeeSummary
            };
            await _userSummaryRepo.AddOne(summary);
            return new Result<EmployeeSummaryRequestModel>
            {
                Message = "Summary Added Successfully",
                Success = true

            };

        }
        public async Task<Result<EmployeeEducationRequestModel>> AddEmployeeEducation(EmployeeEducationRequestModel educationDetails, string userId, string companyId)
        {
            EducationDetails educationDetail = new EducationDetails()
            {
                CompanyId = companyId,
                UserId = userId,
                EducationTitle = educationDetails.EducationTitle,
                CollegeName = educationDetails.CollegeName,
                StartDate = educationDetails.StartDate,
                EndDate = educationDetails.EndDate,
                Type = educationDetails.Type,
            };
            await _educationDetailsRepo.AddOne(educationDetail);
            return new Result<EmployeeEducationRequestModel>
            {
                MethodResult = educationDetails,
                Message = "Education Details Added",
                Success = true

            };
        }

        public async Task<Result<EmployeeCertificationRequestModel>> AddEmployeeCertification(EmployeeCertificationRequestModel cerificationDetails, string userId, string companyId)
        {
            CertificationDetails certificatinDetail = new CertificationDetails()
            {
                CompanyId = companyId,
                UserId = userId,
                CertificationTitle = cerificationDetails.CertificationTitle,
                OrganisationName = cerificationDetails.OrganisationName,
                StartDate = cerificationDetails.StartDate,
                EndDate = cerificationDetails.EndDate,
                Mode = cerificationDetails.Mode,
            };
            await _certificationDetailsRepo.AddOne(certificatinDetail);
            return new Result<EmployeeCertificationRequestModel>
            {
                MethodResult = cerificationDetails,
                Message = "Certification Details Added",
                Success = true
            };
        }

        public async Task<List<EmployeeEducationRequestModel>> GetEmployeeEducationDetails(string userId)
        {
            IEnumerable<EducationDetails> list = await _educationDetailsRepo.GetAll(x => x.UserId == userId);
            return _mapper.Map<List<EmployeeEducationRequestModel>>(list);
        }

        public async Task<List<EmployeeCertificationRequestModel>> GetEmployeeCertificationDetails(string userId)
        {
            IEnumerable<CertificationDetails> list = await _certificationDetailsRepo.GetAll(x => x.UserId == userId);
            return _mapper.Map<List<EmployeeCertificationRequestModel>>(list);
        }

        public async Task<EmployeeSummaryRequestModel> GetEmployeeSummary(string userId)
        {
            UserSummary summary = await _userSummaryRepo.FirstOrDefault(x => x.UserId == userId);
            return new EmployeeSummaryRequestModel()
            {
                EmployeeSummary = summary.EmployeeSummary
            };
        }
    }
}

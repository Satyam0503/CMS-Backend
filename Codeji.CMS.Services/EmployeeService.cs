using System.Linq.Expressions;
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
        readonly IMongoDbRepository<EmployeeSummary> _employeeSummaryRepo;
        readonly IMapper _mapper;

        public EmployeeService(IMongoDbRepository<EducationDetails> educationDetailsRepo, IMapper mapper, IMongoDbRepository<CertificationDetails> certificationDetailsRepo, IMongoDbRepository<EmployeeSummary> userSummary)
        {
            _educationDetailsRepo = educationDetailsRepo;
            _mapper = mapper;
            _certificationDetailsRepo = certificationDetailsRepo;
            _employeeSummaryRepo = userSummary;
        }

        public async Task<Result<EmployeeSummaryRequestModel>> AddEditEmployeeSummary(EmployeeSummaryRequestModel userSummary, string userId, string companyId)
        {
            Expression<Func<EmployeeSummary, bool>> whereCondition = x => userId == x.UserId && x.Id == userSummary.SummaryId && x.CompanyId == companyId;
            EmployeeSummary? employeesummary = await _employeeSummaryRepo.FirstOrDefault(whereCondition);
            bool success = false;

            if (employeesummary == null)
            {
                EmployeeSummary summary = new EmployeeSummary()
                {
                    CompanyId = companyId,
                    UserId = userId,
                    Summary = userSummary.Summary
                };
                Result result = await _employeeSummaryRepo.AddOne(summary);
                success = result.Success;
            }
            else
            {

                employeesummary.Summary = userSummary.Summary;
                Result result = await _employeeSummaryRepo.Update(whereCondition, employeesummary);
                success = result.Success;

            }
            return new Result<EmployeeSummaryRequestModel>
            {
                Message = "Summary Added Successfully",
                Success = success

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
            EmployeeSummary summary = await _employeeSummaryRepo.FirstOrDefault(x => x.UserId == userId);
            return new EmployeeSummaryRequestModel()
            {
                Summary = summary.Summary
            };
        }
    }
}

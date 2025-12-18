using Core.Entities;
using Core.Interfaces.Hierarchy;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Hierarchy
{
    public class EmployeeHierarchyRepository : IEmployeeHierarchyRepository
    {
        private readonly string _connectionString;

        public EmployeeHierarchyRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<IEnumerable<Employees>> GetDirectReportsAsync(Guid managerId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT * FROM employees
                WHERE manager_id = @managerId
                ORDER BY employee_name";

            return await connection.QueryAsync<Employees>(sql, new { managerId });
        }

        public async Task<IEnumerable<Employees>> GetAllSubordinatesAsync(Guid managerId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                WITH RECURSIVE subordinates AS (
                    -- Base case: direct reports
                    SELECT * FROM employees
                    WHERE manager_id = @managerId

                    UNION ALL

                    -- Recursive case: reports of reports
                    SELECT e.* FROM employees e
                    INNER JOIN subordinates s ON e.manager_id = s.id
                )
                SELECT * FROM subordinates
                ORDER BY reporting_level, employee_name";

            return await connection.QueryAsync<Employees>(sql, new { managerId });
        }

        public async Task<IEnumerable<Employees>> GetReportingChainAsync(Guid employeeId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                WITH RECURSIVE reporting_chain AS (
                    -- Base case: the employee's manager
                    SELECT m.* FROM employees e
                    INNER JOIN employees m ON e.manager_id = m.id
                    WHERE e.id = @employeeId

                    UNION ALL

                    -- Recursive case: manager's manager
                    SELECT m.* FROM employees m
                    INNER JOIN reporting_chain rc ON rc.manager_id = m.id
                )
                SELECT * FROM reporting_chain
                ORDER BY reporting_level DESC";

            return await connection.QueryAsync<Employees>(sql, new { employeeId });
        }

        public async Task<bool> IsInReportingChainAsync(Guid managerId, Guid employeeId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                WITH RECURSIVE reporting_chain AS (
                    -- Base case: the employee's manager
                    SELECT manager_id, id FROM employees
                    WHERE id = @employeeId

                    UNION ALL

                    -- Recursive case: manager's manager
                    SELECT e.manager_id, e.id FROM employees e
                    INNER JOIN reporting_chain rc ON rc.manager_id = e.id
                    WHERE e.manager_id IS NOT NULL
                )
                SELECT EXISTS (
                    SELECT 1 FROM reporting_chain
                    WHERE manager_id = @managerId OR id = @managerId
                )";

            return await connection.QuerySingleAsync<bool>(sql, new { managerId, employeeId });
        }

        public async Task<IEnumerable<OrgTreeNode>> GetOrgTreeAsync(Guid companyId, Guid? rootEmployeeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // First, get all employees for the company
            var sql = @"
                SELECT
                    e.id,
                    e.employee_name,
                    e.employee_id,
                    e.email,
                    e.department,
                    e.designation,
                    e.manager_id,
                    e.reporting_level,
                    (SELECT COUNT(*) FROM employees WHERE manager_id = e.id) AS direct_reports_count
                FROM employees e
                WHERE e.company_id = @companyId";

            if (rootEmployeeId.HasValue)
            {
                sql += " AND (e.id = @rootEmployeeId OR e.manager_id IN (SELECT id FROM employees WHERE manager_id = @rootEmployeeId))";
            }

            sql += " ORDER BY e.reporting_level, e.employee_name";

            var employees = (await connection.QueryAsync<OrgTreeNode>(sql, new { companyId, rootEmployeeId })).ToList();

            // Build the tree structure
            var lookup = employees.ToDictionary(e => e.Id);
            var rootNodes = new List<OrgTreeNode>();

            foreach (var employee in employees)
            {
                if (employee.ManagerId.HasValue && lookup.TryGetValue(employee.ManagerId.Value, out var parent))
                {
                    parent.Children.Add(employee);
                }
                else if (!employee.ManagerId.HasValue || (rootEmployeeId.HasValue && employee.Id == rootEmployeeId.Value))
                {
                    rootNodes.Add(employee);
                }
            }

            // Calculate total subordinates count
            foreach (var node in rootNodes)
            {
                CalculateTotalSubordinates(node);
            }

            return rootNodes;
        }

        private int CalculateTotalSubordinates(OrgTreeNode node)
        {
            var total = node.Children.Count;
            foreach (var child in node.Children)
            {
                total += CalculateTotalSubordinates(child);
            }
            node.TotalSubordinatesCount = total;
            return total;
        }

        public async Task<IEnumerable<Employees>> GetManagersAsync(Guid? companyId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT * FROM employees WHERE is_manager = TRUE";

            if (companyId.HasValue)
            {
                sql += " AND company_id = @companyId";
            }

            sql += " ORDER BY employee_name";

            return await connection.QueryAsync<Employees>(sql, new { companyId });
        }

        public async Task<IEnumerable<Employees>> GetTopLevelEmployeesAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT * FROM employees
                WHERE company_id = @companyId AND manager_id IS NULL
                ORDER BY employee_name";

            return await connection.QueryAsync<Employees>(sql, new { companyId });
        }

        public async Task UpdateManagerAsync(Guid employeeId, Guid? managerId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                UPDATE employees
                SET manager_id = @managerId, updated_at = NOW()
                WHERE id = @employeeId";

            await connection.ExecuteAsync(sql, new { employeeId, managerId });
        }

        public async Task<int> GetDirectReportsCountAsync(Guid managerId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = "SELECT COUNT(*) FROM employees WHERE manager_id = @managerId";

            return await connection.QuerySingleAsync<int>(sql, new { managerId });
        }

        public async Task<int> GetAllSubordinatesCountAsync(Guid managerId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                WITH RECURSIVE subordinates AS (
                    SELECT id FROM employees
                    WHERE manager_id = @managerId

                    UNION ALL

                    SELECT e.id FROM employees e
                    INNER JOIN subordinates s ON e.manager_id = s.id
                )
                SELECT COUNT(*) FROM subordinates";

            return await connection.QuerySingleAsync<int>(sql, new { managerId });
        }

        public async Task<bool> WouldCreateCircularReferenceAsync(Guid employeeId, Guid managerId)
        {
            // If the proposed manager is the employee themselves, it's circular
            if (employeeId == managerId)
            {
                return true;
            }

            using var connection = new NpgsqlConnection(_connectionString);

            // Check if the employee is anywhere in the proposed manager's reporting chain
            const string sql = @"
                WITH RECURSIVE reporting_chain AS (
                    -- Base case: start from the proposed manager
                    SELECT id, manager_id FROM employees
                    WHERE id = @managerId

                    UNION ALL

                    -- Recursive case: go up the chain
                    SELECT e.id, e.manager_id FROM employees e
                    INNER JOIN reporting_chain rc ON rc.manager_id = e.id
                    WHERE e.manager_id IS NOT NULL
                )
                SELECT EXISTS (
                    SELECT 1 FROM reporting_chain WHERE id = @employeeId
                )";

            return await connection.QuerySingleAsync<bool>(sql, new { employeeId, managerId });
        }

        public async Task<IEnumerable<Employees>> GetEmployeesByReportingLevelAsync(Guid companyId, int level)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT * FROM employees
                WHERE company_id = @companyId AND reporting_level = @level
                ORDER BY employee_name";

            return await connection.QueryAsync<Employees>(sql, new { companyId, level });
        }
    }
}

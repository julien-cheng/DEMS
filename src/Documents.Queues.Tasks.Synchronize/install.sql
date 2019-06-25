GO
/****** Object:  UserDefinedFunction [dbo].[ufDMSSynchronizeAccount]    Script Date: 4/18/2018 8:29:32 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Andy
-- Create date: 12/29/2017
-- Description:	PCMS-DMS-Synchronization-Account
-- =============================================
Create FUNCTION [dbo].[ufDMSSynchronizeAccount] 
(	
	-- Add the parameters for the function here
	@CountyID int, 
	@SSOAccountID int
)
RETURNS TABLE 
AS
RETURN 
(
	select
		e.FirstName,
		e.LastName,
		a.EmailAddress,
		concat(
			case 
				when exists (select 1 from EmployeeRole er inner join [Role] r on er.RoleID=r.RoleID
					where er.EmployeeID=e.EmployeeID and r.HardcodeLink='CasesSealed') then
					'g:ViewSealed '
				else null
			end, 
			case 
				when exists (select 1 from EmployeeRole er inner join [Role] r on er.RoleID=r.RoleID
					where er.EmployeeID=e.EmployeeID and r.HardcodeLink='CasesViewOwn') then 
					concat('g:ViewOwn{', e.EmployeeID, '} ')
				else null
			end,
			case 
				when exists (select 1 from EmployeeRole er inner join [Role] r on er.RoleID=r.RoleID
					where er.EmployeeID=e.EmployeeID and r.HardcodeLink='CasesEditOwn') then 
					concat('g:EditOwn{', e.EmployeeID, '} ')
				else null
			end,
			case 
				when exists (select 1 from EmployeeRole er inner join [Role] r on er.RoleID=r.RoleID
					where er.EmployeeID=e.EmployeeID and r.HardcodeLink='InvestigationsViewConfidential') then 
					'g:ViewConfidential '
				else null
			end,
			case 
				when exists (select 1 from EmployeeRole er inner join [Role] r on er.RoleID=r.RoleID
					where er.EmployeeID=e.EmployeeID and r.HardcodeLink='CasesViewUnassigned') then 
					'g:ViewUnassigned '
				else null
			end,
			case 
				when exists (select 1 from EmployeeRole er inner join [Role] r on er.RoleID=r.RoleID
					where er.EmployeeID=e.EmployeeID and r.HardcodeLink='CasesEditUnassigned') then 
					'g:EditUnassigned '
				else null
			end,
			case 
				when exists (select 1 from EmployeeRole er inner join [Role] r on er.RoleID=r.RoleID
					where er.EmployeeID=e.EmployeeID and r.HardcodeLink='CasesViewAll') then 
					'g:ViewAll '
				else null
			end,
			case 
				when exists (select 1 from EmployeeRole er inner join [Role] r on er.RoleID=r.RoleID
					where er.EmployeeID=e.EmployeeID and r.HardcodeLink='CasesEditAll') then 
					'g:EditAll '
				else null
			end,
			case 
				when exists (select 1 from EmployeeRole er inner join [Role] r on er.RoleID=r.RoleID
					where er.EmployeeID=e.EmployeeID and r.HardcodeLink='DataAdministrator') then 
					'g:DataAdministrator '
				else null
			end,
			case 
				when exists (select 1 from EmployeeRole er inner join [Role] r on er.RoleID=r.RoleID
					where er.EmployeeID=e.EmployeeID and r.HardcodeLink='DocumentsAdministrator') then 
					'g:DocumentsAdministrator '
				else null
			end,
			case 
				when exists (select 1 from EmployeeRole er inner join [Role] r on er.RoleID=r.RoleID
					where er.EmployeeID=e.EmployeeID and r.HardcodeLink='SystemAdministrator') then 
					'g:SystemAdministrator '
				else null
			end,
			concat('o:PCMS:', e.CountyID)
		) as SecurityIdentifiers

	from 
		Employee e
		inner join Account a
			on e.EmployeeID=a.EmployeeID
			and a.SSOAccountID is not null

	where
		a.SSOAccountID=@SSOAccountID
		and a.CountyID=@CountyID
		and e.CountyID=@CountyID

)
GO

/****** Object:  UserDefinedFunction [dbo].[ufDMSSynchronizeDefendant]    Script Date: 4/18/2018 8:29:48 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Andy
-- Create date: 12/29/2017
-- Description:	PCMS-DMS-Synchronization-Defendant
-- =============================================
create FUNCTION [dbo].[ufDMSSynchronizeDefendant]
(	
	-- Add the parameters for the function here
	@CountyID int, 
	@DefendantID int
)
RETURNS TABLE 
AS
RETURN 
(
	select
		d.DefendantID,
		case
			when pa.PersonAliasID is null then p.FirstName
			else pa.FirstName
		end FirstName,
		case
			when pa.PersonAliasID is null then p.LastName
			else pa.LastName
		end LastName,
		d.CaseNumber,
		cs.DisplayName as CaseStatus,
		cs.IsClosed as IsClosed,

		case
			when d.Active=0 then 1
			else 0
		end as IsDeleted,

		defatty.FirstName DefenseFirstName,
		defatty.LastName DefenseLastName,
		defatty.EmailAddress DefenseEmail,

	--- Read ACLs

		-- build an acl for assignments
		-- it will look like "g:ViewOwn{97347} g:ViewOwn{105482} g:ViewOwn{110308} g:ViewAll" where each number is an assigned employeeID
		-- if the case is unassigned, it will also get a "g:ViewUnassigned" tag
		concat('(basic) ',
			(
				-- foreach ADA and SupportStaff, create an entry g:ViewOwn{EmployeeID}
				select concat('g:ViewOwn{', EmployeeID, '} ') from (
					select EmployeeID from DefendantSupportStaff where DefendantID=d.DefendantID
					union select ADAEmployeeID as EmployeeID from DefendantADA where DefendantID=d.DefendantID
				) unioned
				for XML PATH('')
			), 
			'g:ViewAll ', -- also allow someone with the ViewAll role
			case 
				when not exists(
					select EmployeeID from (
						select EmployeeID from DefendantSupportStaff where DefendantID=d.DefendantID
						union select ADAEmployeeID as EmployeeID from DefendantADA where DefendantID=d.DefendantID
					) unioned) then 'g:ViewUnassigned '
				else ''
			end		
		) as acl_read_0,

		-- build an acl for sealed cases. either everyone can view it or only those with the ViewSealed role
		case 
			when d.SealTypeID is null then null 
			else '(sealed) g:ViewSealed' 
		end as acl_read_1,

		-- build an acl for confidential cases. either everyone can view it or only those with the ViewConfidential role
		case 
			when it.IsConfidential = 1 then '(confidential) g:ViewConfidential' 
			else null 
		end as acl_read_2,

		concat('(default) o:PCMS:', d.CountyID, ' u:system') as acl_read_3,

	--- Write ACLs

		-- build an acl for assignments
		concat('(basic) ',
			(
				-- foreach ADA and SupportStaff, create an entry g:ViewOwn{EmployeeID} g:EditOwn{EmployeeID}
				select concat('g:EditOwn{', EmployeeID, '} ') from (
					select EmployeeID from DefendantSupportStaff where DefendantID=d.DefendantID
					union select ADAEmployeeID as EmployeeID from DefendantADA where DefendantID=d.DefendantID
				) unioned
				for XML PATH('')
			), 
			'g:EditAll', -- also allow someone with the ViewAll role
			case 
				when not exists(
					select EmployeeID from (
						select EmployeeID from DefendantSupportStaff where DefendantID=d.DefendantID
						union select ADAEmployeeID as EmployeeID from DefendantADA where DefendantID=d.DefendantID
					) unioned) then 'g:ViewUnassigned g:EditUnassigned'
				else ''
			end		
		) as acl_write_0,

		concat('(default) o:PCMS:', d.CountyID, ' u:system') as acl_write_1

	from
		Defendant d
		inner join Person p
			on d.PersonID=p.PersonID
		left join PersonAlias pa
			on d.PrimaryPersonAliasID=pa.PersonAliasID
		left join CaseStatus cs
			on d.CaseStatusID=cs.CaseStatusID 
		left join InvestigationType it
			on d.InvestigationTypeID=it.InvestigationTypeID
		left join (
			select top 1 dda.DefendantID, da.FirstName, da.LastName, da.EmailAddress
			from
				DefendantDefenseAttorney dda
				inner join DefenseAttorney da
					on dda.DefenseAttorneyID=da.DefenseAttorneyID
			where
				dda.DefendantID=@DefendantID
		) defatty
			on defatty.DefendantID=d.DefendantID
			
		
	where
		d.DefendantID=@DefendantID
		and d.CountyID=@CountyID
)




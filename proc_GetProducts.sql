USE [OnlineShopingApp]
GO
/****** Object:  StoredProcedure [dbo].[proc_GetProducts]    Script Date: 18-08-2019 11:20:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
ALTER PROCEDURE [dbo].[proc_GetProducts]
@PageIndex int,
@RecordCount int output
AS
BEGIN

Declare @PageSize int =10;
SELECT ROW_NUMBER() over (order by ProductId asc) RowNum,
     [ProductId],[ProductName],[ProductDesc],[ProductPrice],[ProductImage],[CategoryId],[CreatedDate]
	  into #Results
  FROM [OnlineShopingApp].[dbo].[Product];

  SELECT [ProductId],[ProductName],[ProductDesc],[ProductPrice],[ProductImage],[CategoryId],[CreatedDate] FROM #Results
      WHERE RowNum BETWEEN(@PageIndex -1) * @PageSize + 1 AND(((@PageIndex -1) * @PageSize + 1) + @PageSize) - 1;

  SELECT @RecordCount = COUNT(*) FROM #Results
     
  DROP TABLE #Results

END
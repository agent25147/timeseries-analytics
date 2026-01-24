import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AgGridAngular } from 'ag-grid-angular';
import {
  ColDef,
  GridApi,
  GridReadyEvent,
  IServerSideDatasource,
  IServerSideGetRowsParams,
} from 'ag-grid-community';
import { TimeseriesService } from '../../services/timeseries.service';
import { AgGridRequest, ColumnFilter, SortModel } from '../../models/ag-grid-request.model';
import { ModuleRegistry, AllCommunityModule } from 'ag-grid-community';
import { themeQuartz } from 'ag-grid-community';
import { SideBarModule } from 'ag-grid-enterprise';
import { RowGroupingPanelModule } from 'ag-grid-enterprise';
import { ColumnsToolPanelModule } from 'ag-grid-enterprise';
import { FiltersToolPanelModule } from 'ag-grid-enterprise';

ModuleRegistry.registerModules([ AllCommunityModule ]);
ModuleRegistry.registerModules([ SideBarModule ]);
ModuleRegistry.registerModules([ RowGroupingPanelModule ]);
ModuleRegistry.registerModules([ ColumnsToolPanelModule ]);
ModuleRegistry.registerModules([ FiltersToolPanelModule ]);


@Component({
  selector: 'app-data-grid',
  standalone: true,
  imports: [CommonModule, AgGridAngular],
  templateUrl: './data-grid.component.html',
  styleUrls: ['./data-grid.component.css']
})
export class DataGridComponent implements OnInit {
   theme = themeQuartz;
  private gridApi!: GridApi;

  // Grid options
  public rowModelType: 'serverSide' = 'serverSide';
  public cacheBlockSize = 100;
  public maxBlocksInCache = 10;
  public pagination = true;
  public paginationPageSize = 100;
  public paginationPageSizeSelector = [50, 100, 200, 500];

  // Column definitions
 public columnDefs: ColDef[] = [
  {
    field: 'transaction_id',
    headerName: 'Transaction ID',
    width: 280,
    filter: 'agTextColumnFilter',
    sortable: true
  },
  {
    field: 'timestamp',
    headerName: 'Timestamp',
    width: 180,
    filter: 'agDateColumnFilter',
    sortable: true,
    valueFormatter: (params) => {
      if (params.value) {
        return new Date(params.value).toLocaleString();
      }
      return '';
    }
  },
  {
    field: 'region',
    headerName: 'Region',
    width: 150,
    filter: 'agTextColumnFilter',
    sortable: true,
    enableRowGroup: true,
    enablePivot: true
  },
  {
    field: 'category',
    headerName: 'Category',
    width: 150,
    filter: 'agTextColumnFilter',
    sortable: true,
    enableRowGroup: true,
    enablePivot: true
  },
  {
    field: 'status',
    headerName: 'Status',
    width: 130,
    filter: 'agTextColumnFilter',
    sortable: true,
    enableRowGroup: true,
    enablePivot: true
  },
  {
    field: 'product_name',
    headerName: 'Product',
    width: 200,
    filter: 'agTextColumnFilter',
    sortable: true
  },
  {
    field: 'amount',
    headerName: 'Amount',
    width: 130,
    filter: 'agNumberColumnFilter',
    sortable: true,
    aggFunc: 'sum',
    valueFormatter: (params) => {
      if (params.value != null) {
        return '$' + params.value.toFixed(2);
      }
      return '';
    }
  },
  {
    field: 'quantity',
    headerName: 'Quantity',
    width: 120,
    filter: 'agNumberColumnFilter',
    sortable: true,
    aggFunc: 'sum'
  },
  {
    field: 'user_id',
    headerName: 'User ID',
    width: 120,
    filter: 'agNumberColumnFilter',
    sortable: true
  },
  {
    field: 'payment_method',
    headerName: 'Payment Method',
    width: 150,
    filter: 'agTextColumnFilter',
    sortable: true,
    enablePivot: true
  },
  {
    field: 'bill_to_name',
    headerName: 'Bill To',
    width: 200,
    filter: 'agTextColumnFilter',
    sortable: true
  },
  {
    field: 'address',
    headerName: 'Address',
    width: 250,
    filter: 'agTextColumnFilter',
    sortable: true
  },
  {
    field: 'shipping_lane',
    headerName: 'Shipping Lane',
    width: 150,
    filter: 'agTextColumnFilter',
    sortable: true
  },
  {
    field: 'scac_code',
    headerName: 'SCAC Code',
    width: 120,
    filter: 'agTextColumnFilter',
    sortable: true
  },
  {
    field: 'currency_code',
    headerName: 'Currency',
    width: 100,
    filter: 'agTextColumnFilter',
    sortable: true
  },
  {
    field: 'weight',
    headerName: 'Weight (kg)',
    width: 130,
    filter: 'agNumberColumnFilter',
    sortable: true,
    aggFunc: 'sum',
    valueFormatter: (params) => {
      if (params.value != null) {
        return params.value.toFixed(2) + ' kg';
      }
      return '';
    }
  },
  {
    field: 'invoice_number',
    headerName: 'Invoice #',
    width: 280,
    filter: 'agTextColumnFilter',
    sortable: true
  }
  ];


  // Default column definition
  public defaultColDef: ColDef = {
    flex: 1,
    minWidth: 100,
    resizable: true,
    sortable: false,
    filter: false
  };

  // Auto group column for row grouping
  public autoGroupColumnDef: ColDef = {
    minWidth: 200,
    cellRendererParams: {
      suppressCount: false
    }
  };

  // Sidebar configuration for pivot panel
  public sideBar = {
    toolPanels: [
      {
        id: 'columns',
        labelDefault: 'Columns',
        labelKey: 'columns',
        iconKey: 'columns',
        toolPanel: 'agColumnsToolPanel',
        toolPanelParams: {
          suppressRowGroups: false,
          suppressValues: false,
          suppressPivots: false,
          suppressPivotMode: false
        }
      },
      {
        id: 'filters',
        labelDefault: 'Filters',
        labelKey: 'filters',
        iconKey: 'filter',
        toolPanel: 'agFiltersToolPanel'
      }
    ],
    defaultToolPanel: ''
  };

  constructor(private timeseriesService: TimeseriesService) {}

  ngOnInit(): void {}

  onGridReady(params: GridReadyEvent): void {
    this.gridApi = params.api;

    // Create server-side datasource
    const datasource: IServerSideDatasource = {
      getRows: (params: IServerSideGetRowsParams) => {
        console.log('Server-side request:', params.request);
        
        // Build AG-Grid request
        const request = this.buildRequest(params);
      
        // Call backend
        this.timeseriesService.query(request).subscribe({
          next: (response) => {
            console.log('Server response:', response);
      
            // Check if this is a grouped request
            const isGroupRequest = request.rowGroupCols && request.rowGroupCols.length > 0;
            
            if (isGroupRequest) {
              // For grouped data, don't set rowCount to trigger on-demand loading
              params.success({
                rowData: response.data,
                rowCount: response.data.length < (request.endRow - request.startRow) 
                  ? request.startRow + response.data.length 
                  : undefined
              });
            } else {
              // For non-grouped data, use the total count
              params.success({
                rowData: response.data,
                rowCount: response.lastRow
              });
            }
          },
          error: (error) => {
            console.error('Error fetching data:', error);
            params.fail();
          }
        });
      }
    };

    // Set datasource
    this.gridApi.setGridOption('serverSideDatasource', datasource);
  }

  private buildRequest(params: IServerSideGetRowsParams): AgGridRequest {
    const request: AgGridRequest = {
      startRow: params.request.startRow || 0,
      endRow: params.request.endRow || 100,
      filterModel: this.buildFilterModel(params.request.filterModel),
      sortModel: this.buildSortModel(params.request.sortModel),
      rowGroupCols: params.request.rowGroupCols?.map(col => col.field) || [],
      groupKeys: params.request.groupKeys || [],
      valueCols: params.request.valueCols?.map(col => col.field) || [],
      pivotCols: params.request.pivotCols?.map(col => col.field) || [],
      pivotMode: params.request.pivotMode || false
    };

    return request;
  }

  private buildFilterModel(filterModel: any): ColumnFilter[] {
    const filters: ColumnFilter[] = [];

    if (!filterModel) return filters;

    for (const [columnName, filter] of Object.entries<any>(filterModel)) {
      if (filter.filterType === 'text') {
        filters.push({
          columnName,
          filterType: 'text',
          type: filter.type,
          filter: filter.filter
        });
      } else if (filter.filterType === 'number') {
        filters.push({
          columnName,
          filterType: 'number',
          type: filter.type,
          filter: filter.filter?.toString(),
          filterTo: filter.filterTo?.toString()
        });
      } else if (filter.filterType === 'date') {
        filters.push({
          columnName,
          filterType: 'date',
          dateFrom: filter.dateFrom,
          dateTo: filter.dateTo
        });
      }
    }

    return filters;
  }

  private buildSortModel(sortModel: any[]): SortModel[] {
    if (!sortModel) return [];

    return sortModel.map(sort => ({
      colId: sort.colId,
      sort: sort.sort
    }));
  }

  // Utility methods for testing
  onRefresh(): void {
    this.gridApi.refreshServerSide({ purge: true });
  }

  onClearFilters(): void {
    this.gridApi.setFilterModel(null);
  }

  onClearSorting(): void {
    this.gridApi.applyColumnState({
      defaultState: { sort: null }
    });
  }
}

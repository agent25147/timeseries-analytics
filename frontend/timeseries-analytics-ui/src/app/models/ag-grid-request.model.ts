export interface AgGridRequest {
  startRow: number;
  endRow: number;
  filterModel: ColumnFilter[];
  sortModel: SortModel[];
  rowGroupCols: (string | undefined)[] ;
  groupKeys: (string | undefined)[] ;
  valueCols: (string | undefined)[] ;
  pivotCols: (string | undefined)[] ;
  pivotMode: boolean;
}

export interface ColumnFilter {
  columnName: string;
  filterType: string;
  type?: string;
  filter?: string;
  filterTo?: string;
  dateFrom?: string;
  dateTo?: string;
}

export interface SortModel {
  colId: string;
  sort: string;
}

export interface AgGridResponse<T> {
  data: T[];
  lastRow: number;
  secondaryColumns?: string[];
}

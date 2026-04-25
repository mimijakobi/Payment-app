export interface DbColumn {
  name: string;
  note: string;
}

export interface DbTable {
  name: string;
  color: string;
  columns: DbColumn[];
}

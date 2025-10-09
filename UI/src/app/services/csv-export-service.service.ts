import { Injectable } from '@angular/core';
import { Papa } from 'ngx-papaparse';

@Injectable({
  providedIn: 'root'
})
export class CsvExportServiceService {

  constructor(private papa: Papa) { }

  exportToCsv<T extends Record<string, any>>(filename: string, data: T[],headers: { [key: string]: string },totalColumns: (keyof T)[]) {
    const formattedData = data.map(row => {
      const formattedRow: { [key: string]: any } = {};
      for (const key in headers) {
        if (headers.hasOwnProperty(key)) {
          const value = row[key as keyof T];
          const valueStr = String(value); // Cast value to string
          if (this.isIsoDateString(valueStr)) {
            formattedRow[headers[key]] = this.formatDate(valueStr);
          } else {
            formattedRow[headers[key]] = value;
          }
        }
      }
      return formattedRow;
    });

    const totals: { [column: string]: string } = {};
    totalColumns.forEach(column => {
      const total = data.reduce((sum, row) => sum + (row[column] || 0), 0);
      totals[headers[column as string]] = `Total = ${total}`;
    });

    const totalRow: { [key: string]: any } = {};
    Object.keys(headers).forEach(headerKey => {
      const header = headers[headerKey];
      if (totals.hasOwnProperty(header)) {
        totalRow[header] = totals[header];
      } else {
        totalRow[header] = '';
      }
    });

    const totalRowIndex = formattedData.length;
    formattedData.splice(totalRowIndex, 0, totalRow);

    const csv = this.papa.unparse(formattedData);
    this.downloadFile(csv, filename);
  }

  private formatDate(dateString: string): string {
    const date = new Date(dateString);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private isIsoDateString(value: string): boolean {
    const isoDatePattern = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{3})?Z?$/;
    return isoDatePattern.test(value);
  }

  private downloadFile(csvData: string, filename: string) {
    const blob = new Blob([csvData], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);

    link.setAttribute('href', url);
    link.setAttribute('download', filename);
    link.style.visibility = 'hidden';

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
}

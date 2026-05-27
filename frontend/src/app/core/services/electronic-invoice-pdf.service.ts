import { Injectable } from '@angular/core';
import { jsPDF } from 'jspdf';
import autoTable from 'jspdf-autotable';
import { ElectronicInvoice } from '../models';

type PdfDoc = jsPDF & { lastAutoTable: { finalY: number } };

/** Carta US: 215.9 × 279.4 mm */
const PAGE_W = 215.9;
const MARGIN = 14;
const CONTENT_W = PAGE_W - MARGIN * 2;
const RIGHT_X = MARGIN + CONTENT_W;
const COL_DESC = 94;
const COL_QTY = 22;
const COL_UNIT = 36;
const COL_TOTAL = CONTENT_W - COL_DESC - COL_QTY - COL_UNIT;

@Injectable({ providedIn: 'root' })
export class ElectronicInvoicePdfService {
  print(invoice: ElectronicInvoice): void {
    const url = this.createBlobUrl(invoice);
    const popup = window.open(url, '_blank');

    if (popup) {
      const triggerPrint = () => {
        try {
          popup.focus();
          popup.print();
        } catch {
          /* El visor PDF del navegador muestra su propio boton Imprimir */
        }
      };
      popup.addEventListener('load', () => setTimeout(triggerPrint, 400));
      setTimeout(triggerPrint, 1200);
      setTimeout(() => URL.revokeObjectURL(url), 120_000);
      return;
    }

    this.printViaIframe(url);
  }

  download(invoice: ElectronicInvoice): void {
    const doc = this.buildDocument(invoice);
    doc.save(this.fileName(invoice));
  }

  private createBlobUrl(invoice: ElectronicInvoice): string {
    const doc = this.buildDocument(invoice);
    return URL.createObjectURL(doc.output('blob'));
  }

  private printViaIframe(url: string): void {
    const iframe = document.createElement('iframe');
    iframe.setAttribute('title', 'Factura electronica');
    iframe.style.cssText = 'position:fixed;right:0;bottom:0;width:0;height:0;border:0';
    iframe.src = url;
    document.body.appendChild(iframe);

    iframe.onload = () => {
      setTimeout(() => {
        iframe.contentWindow?.focus();
        iframe.contentWindow?.print();
        setTimeout(() => {
          iframe.remove();
          URL.revokeObjectURL(url);
        }, 1500);
      }, 300);
    };
  }

  private fileName(invoice: ElectronicInvoice): string {
    return `${invoice.electronicInvoiceNumber || invoice.documentNumber}.pdf`;
  }

  private buildDocument(invoice: ElectronicInvoice): jsPDF {
    const doc = new jsPDF({ unit: 'mm', format: 'letter' });
    const money = (value: number) =>
      new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', maximumFractionDigits: 0 }).format(
        value
      );
    const issued = invoice.issuedAt
      ? new Date(invoice.issuedAt).toLocaleString('es-CO')
      : new Date(invoice.saleDate).toLocaleString('es-CO');

    const leftBlock = (lines: string[], startY: number, boldFirst = false): number => {
      let y = startY;
      lines.forEach((line, i) => {
        if (!line.trim()) return;
        doc.setFont('helvetica', boldFirst && i === 0 ? 'bold' : 'normal');
        doc.setFontSize(boldFirst && i === 0 ? 10 : 9);
        const wrapped = doc.splitTextToSize(line, CONTENT_W * 0.52);
        doc.text(wrapped, MARGIN, y);
        y += wrapped.length * 4.2;
      });
      return y;
    };

    const rightLine = (label: string, value: string, y: number) => {
      doc.setFont('helvetica', 'normal');
      doc.setFontSize(9);
      const text = `${label}: ${value}`;
      doc.text(text, RIGHT_X, y, { align: 'right', maxWidth: CONTENT_W * 0.46 });
    };

    // Encabezado
    doc.setFillColor(15, 23, 42);
    doc.rect(0, 0, PAGE_W, 28, 'F');
    doc.setTextColor(255, 255, 255);
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(15);
    doc.text('FACTURA ELECTRÓNICA DE VENTA', MARGIN, 12);
    doc.setFontSize(10);
    doc.text(invoice.electronicInvoiceNumber || 'Borrador', MARGIN, 20);

    doc.setTextColor(30, 41, 59);

    const issuerLines = [
      invoice.issuerName,
      `NIT: ${invoice.issuerTaxId ?? '—'}`,
      invoice.issuerAddress ?? '',
      [invoice.issuerPhone, invoice.issuerEmail].filter(Boolean).join(' · ')
    ].filter(l => l.trim());

    const metaY = 38;
    const issuerEndY = leftBlock(issuerLines, metaY, true);

    rightLine('Factura FE', invoice.electronicInvoiceNumber ?? 'Por asignar', metaY);
    rightLine('Venta', invoice.documentNumber, metaY + 5);
    rightLine('Fecha', issued, metaY + 10);
    rightLine('Estado', invoice.electronicInvoiceStatus, metaY + 15);
    rightLine('Pago', invoice.paymentMethod, metaY + 20);

    let y = Math.max(issuerEndY, metaY + 26) + 4;

    // Cliente
    doc.setFillColor(248, 250, 252);
    doc.roundedRect(MARGIN, y, CONTENT_W, 14, 2, 2, 'F');
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(9);
    doc.text(`Cliente: ${invoice.customerName}`, MARGIN + 4, y + 9, {
      maxWidth: invoice.customerTaxId ? CONTENT_W * 0.58 : CONTENT_W - 8
    });
    if (invoice.customerTaxId) {
      doc.setFont('helvetica', 'normal');
      doc.text(`NIT/CC: ${invoice.customerTaxId}`, RIGHT_X - 4, y + 9, {
        align: 'right',
        maxWidth: CONTENT_W * 0.38
      });
    }

    // Detalle
    autoTable(doc, {
      startY: y + 18,
      margin: { left: MARGIN, right: MARGIN },
      tableWidth: CONTENT_W,
      head: [['Descripción', 'Cant.', 'V. unitario', 'Total']],
      body: invoice.lines.map(line => [
        line.description,
        String(line.quantity),
        money(line.unitPrice),
        money(line.lineTotal)
      ]),
      styles: {
        fontSize: 9,
        cellPadding: { top: 2.5, right: 3, bottom: 2.5, left: 3 },
        overflow: 'linebreak',
        valign: 'middle'
      },
      headStyles: {
        fillColor: [14, 165, 233],
        textColor: [15, 23, 42],
        fontStyle: 'bold',
        halign: 'center'
      },
      columnStyles: {
        0: { cellWidth: COL_DESC, halign: 'left' },
        1: { cellWidth: COL_QTY, halign: 'center' },
        2: { cellWidth: COL_UNIT, halign: 'right' },
        3: { cellWidth: COL_TOTAL, halign: 'right' }
      },
      didParseCell: data => {
        if (data.section === 'head' && data.column.index === 0) {
          data.cell.styles.halign = 'left';
        }
      }
    });

    const tableEnd = (doc as PdfDoc).lastAutoTable.finalY + 6;
    const totalsW = COL_UNIT + COL_TOTAL;
    const totalsX = RIGHT_X;

    const totalRow = (label: string, value: string, rowY: number, bold = false) => {
      doc.setFont('helvetica', bold ? 'bold' : 'normal');
      doc.setFontSize(bold ? 12 : 10);
      doc.text(label, totalsX - totalsW, rowY, { align: 'left', maxWidth: totalsW * 0.55 });
      doc.text(value, totalsX, rowY, { align: 'right', maxWidth: totalsW * 0.42 });
    };

    totalRow('Subtotal', money(invoice.subtotal), tableEnd);
    totalRow(`IVA (${(invoice.taxRate * 100).toFixed(0)}%)`, money(invoice.tax), tableEnd + 6);
    totalRow('TOTAL', money(invoice.total), tableEnd + 14, true);

    let footerY = tableEnd + 26;

    if (invoice.cufe) {
      const cufeH = 24;
      doc.setFillColor(240, 249, 255);
      doc.roundedRect(MARGIN, footerY, CONTENT_W, cufeH, 2, 2, 'F');
      doc.setFont('helvetica', 'bold');
      doc.setFontSize(8);
      doc.text('CUFE — Código único de factura electrónica', MARGIN + 4, footerY + 7);
      doc.setFont('helvetica', 'normal');
      doc.setFontSize(7);
      doc.text(invoice.cufe, MARGIN + 4, footerY + 13, {
        maxWidth: CONTENT_W - 8,
        align: 'left',
        lineHeightFactor: 1.35
      });
      footerY += cufeH + 6;
    }

    doc.setFontSize(7);
    doc.setTextColor(100, 116, 139);
    doc.setFont('helvetica', 'normal');
    doc.text(
      'Documento generado por ContaNexo. Representación para archivo y control interno; validación DIAN requiere proveedor tecnológico habilitado.',
      MARGIN,
      Math.max(footerY, 258),
      { maxWidth: CONTENT_W, align: 'justify', lineHeightFactor: 1.3 }
    );

    return doc;
  }
}

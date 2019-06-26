import React, { useState, useEffect, useRef } from 'react';
import { usePdf } from 'react-pdf-js';
import './PDF.css';
import { Document, Page } from 'react-pdf';

export default class PDFViewer extends React.Component {
  constructor(props) {
    super(props);
    this.state = { src: props.src, numPages: null, pageNumber: 1 };
  }
  removeTextLayerOffset() {
    const textLayers = document.querySelectorAll('.react-pdf__Page__textContent');
    textLayers.forEach(layer => {
      const { style } = layer;
      style.top = '0';
      style.left = '0';
      style.bottom = '0';
      style.right = '0';
      style.transform = '';
      style.transform = '';
    });
    const textLayers2 = document.querySelectorAll('.react-pdf__Page__annotations');
    textLayers2.forEach(layer => {
      const { style } = layer;
      style.top = '0';
      style.left = '0';
      style.bottom = '0';
      style.right = '0';
      style.transform = '';
      style.position = 'absolute';
      style.display = 'none';
    });
  }
  onDocumentLoadSuccess = ({ numPages }) => {
    this.setState({ numPages });
    this.removeTextLayerOffset();
  };

  render() {
    const { pageNumber, numPages } = this.state;

    return (
      <div style={{ overflow: 'hidden', height: window.innerHeight - 216 }}>
        <Document file={this.state.src} onLoadSuccess={this.onDocumentLoadSuccess}>
          <Page
            pageNumber={pageNumber}
            height={window.innerHeight - 280}
            onLoadSuccess={this.removeTextLayerOffset}
          />
        </Document>
        <p>
          Page {pageNumber} of {numPages}
        </p>
      </div>
    );
  }
}

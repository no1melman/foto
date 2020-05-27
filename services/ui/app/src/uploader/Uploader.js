import React, { useState, useLayoutEffect } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

import './uploader.scss';
import DisplayFile from './DisplayFile';

const toMb = size => `${(size / 1024 / 1024).toPrecision(2)} MB`;

const mapFiles = files => {
  if (!files) return [];

  const display = [];
  Array.prototype.map.call(files, file => {
    const { name, size, type } = file;
    const url = (
      window.URL ||
      window.webkitURL ||
      window.mozURL
    ).createObjectURL(file);

    display.push({
      name,
      displaySize: toMb(size),
      size,
      type,
      url,
    });
  });

  return display;
};

const Uploader = ({ onUploaded, files }) => {
  const [displayFiles, setDisplayFiles] = useState([]);
  const [rawFiles, setRawFiles] = useState([]);
  const [fileCount, setFileCount] = useState(0);
  const [progress, setProgress] = useState(0);
  const [requestState, setRequestState] = useState('');

  useLayoutEffect(() => {
    let newFiles = Array.prototype.map
      .call(files, (file, i) => {
        console.log(file, rawFiles);
        if (rawFiles.some(x => x.name === file.name)) {
          return null;
        }

        return file;
      })
      .filter(x => x);
    let combined = rawFiles.concat(newFiles);

    setRawFiles(combined);
  }, [files]);

  useLayoutEffect(() => {
    if (fileCount !== rawFiles.length) {
      setFileCount(rawFiles.length);
    }
    setDisplayFiles(mapFiles(rawFiles));
  }, [rawFiles]);

  const uploadFiles = () => {
    setProgress(0);
    const form = new FormData();

    Array.prototype.forEach.call(rawFiles, (file, i) => {
      form.append(`input${i}`, file);
    });

    const xhr = new XMLHttpRequest();
    xhr.addEventListener('progress', e => {
      let progress = 0;
      if (e.total !== 0) {
        progress = parseInt((e.loaded / e.total) * 100, 10);
      }
      setProgress(progress);
    });
    xhr.onreadystatechange = () => {
      if (xhr.readyState === xhr.DONE) {
        setProgress(100);
        if (xhr.status >= 200 || xhr.status < 400) {
          setRequestState('Success');
          onUploaded();
        } else {
          setRequestState('Failed');
        }
      }
    };
    xhr.open('POST', '/api/photos', true);
    xhr.send(form);
  };

  const removeFile = name => e => {
    e.preventDefault();
    e.stopPropagation();

    const filtered = Array.prototype.filter.call(
      rawFiles,
      file => file.name !== name
    );

    setRawFiles(filtered);
    setDisplayFiles(mapFiles(filtered));
  };

  const handleClose = () => {
    setRawFiles([]);
  };

  const classNames = [
    'uploader-container',
    fileCount === 0 ? '' : 'uploader-container-show',
  ].join(' ');

  return (
    <div className={classNames}>
      {displayFiles.map(f => (
        <DisplayFile key={f.name} file={f} onDelete={removeFile} />
      ))}

      <div className="uploader-container__tooltip">
        Upload ({toMb(displayFiles.reduce((r, f) => r + f.size, 0))})
        <span
          className="uploader-container__tooltip-close"
          onClick={handleClose}
        >
          <FontAwesomeIcon icon="times" />
        </span>
      </div>
    </div>
  );
};

export default Uploader;

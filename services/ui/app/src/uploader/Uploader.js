import React, { useState } from 'react';

import FilesDragAndDrop from '../components/FilesDragAndDrop';

const toMb = size => `${(size / 1024 / 1024).toPrecision(2)} MB`;

const mapFiles = files => {
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

const Uploader = ({ onUploaded }) => {
  const [displayFiles, setDisplayFiles] = useState([]);
  const [files, setFiles] = useState([]);
  const [progress, setProgress] = useState(0);
  const [requestState, setRequestState] = useState('');

  const onUpload = files => {
    console.log(files);
    setFiles(files);
    const display = mapFiles(files);
    setDisplayFiles(display);
  };

  const uploadFiles = () => {
    setProgress(0);
    const form = new FormData();

    Array.prototype.forEach.call(files, (file, i) => {
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
      files,
      file => file.name !== name
    );

    onUpload(filtered);
  };

  return (
    <div>
      <FilesDragAndDrop onUpload={onUpload} />

      <div style={{ display: 'flex' }}>
        {displayFiles.map(f => (
          <div key={f.name}>
            <img src={f.url} width={100} />
            <br />
            Name: {f.name}
            <br />
            Size: {f.displaySize}
            <br />
            Type: {f.type}
            <br />
            <button onClick={removeFile(f.name)}>Delete</button>
          </div>
        ))}
      </div>

      <button onClick={uploadFiles}>
        Upload ({toMb(displayFiles.reduce((r, f) => r + f.size, 0))})
      </button>

      <p>{progress !== 0 && progress}</p>

      <p>{progress === 100 && requestState}</p>
    </div>
  );
};

export default Uploader;

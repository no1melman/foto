import React, { useState } from 'react';

import FilesDragAndDrop from './components/FilesDragAndDrop';

export default function App() {
  const [displayFiles, setDisplayFiles] = useState([]);
  const [files, setFiles] = useState([]);
  const [progress, setProgress] = useState(0);

  const onUpload = files => {
    console.log(files);
    setFiles(files);
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
        size: `${(size / 1024 / 1024).toPrecision(2)} MB`,
        type,
        url,
      });
    });
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
        if (xhr.status === 200) {
          console.log(xhr.response);
          console.log(xhr.responseText);
        }
      }
    };
    xhr.open('POST', 'http://localhost:5000/photos', true);
    xhr.send(form);
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
            Size: {f.size}
            <br />
            Type: {f.type}
            <br />
          </div>
        ))}
      </div>

      <button onClick={uploadFiles}>Upload</button>

      <p>{progress !== 0 && progress}</p>

      <p>{progress === 100 && 'Success'}</p>
    </div>
  );
}

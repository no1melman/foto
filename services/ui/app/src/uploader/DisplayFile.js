import React, { useState } from 'react';

import './displayFile.scss';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

const DisplayFile = ({ file, onDelete }) => {
  const { name, url, displaySize, type } = file;

  return (
    <div key={name} className="display-file">
      <img src={url} />
      <div className="display-file_overlay">
        <span className="display-file_overlay-icon">
          <FontAwesomeIcon icon="times" onClick={onDelete(name)} />
        </span>
        <div className="display-file_overlay-content">
          <span className="display-file_overlay-content_top">{name}</span>
          <span className="display-file_overlay-content_middle">{type}</span>
          <span className="display-file_overlay-content_bottom">
            {displaySize}
          </span>
        </div>
      </div>
    </div>
  );
};

export default DisplayFile;
